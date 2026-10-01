"""Keep the Web Forms project in step with the files on disk.

StudyLink.Web is an old-style (non-SDK) Web Application project, which has two chores that
silently break builds when forgotten:

  1. Every .aspx/.ascx/.master needs a .designer.cs declaring its server controls. Visual Studio
     writes these when you save in the designer; MSBuild never does.
  2. Every file must be listed in the .csproj (Compile for code, Content for markup and assets).

Run after adding, renaming or editing markup:   python tools/sync_web_project.py
It rewrites only the designer files and the block between the GENERATED markers in the .csproj.
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WEB = ROOT
CSPROJ = os.path.join(WEB, "StudyLink.Web.csproj")

MARKUP_EXT = (".aspx", ".ascx", ".master")
CODE_PARENTS = MARKUP_EXT + (".asax", ".ashx")
CONTENT_EXT = (".aspx", ".ascx", ".master", ".ashx", ".asax", ".css", ".js", ".png", ".jpg", ".jpeg",
               ".gif", ".svg", ".ico", ".webp", ".woff", ".woff2", ".sample")
EXCLUDED_DIRS = {"bin", "obj", ".vs", "Logs", "MailDrop", "Invoices", "Backups"}
# The web project is the repository root, so the other projects and the non-site folders sit beside its pages.
NOT_SITE = {".git", ".claude", "Data", "Services", "Tests", "TestResults", "legacy", "docs", "database", "tools", "branding", "packages"}

HTML_TYPES = {
    "form": "System.Web.UI.HtmlControls.HtmlForm",
    "a": "System.Web.UI.HtmlControls.HtmlAnchor",
    "img": "System.Web.UI.HtmlControls.HtmlImage",
    "head": "System.Web.UI.HtmlControls.HtmlHead",
    "title": "System.Web.UI.HtmlControls.HtmlTitle",
    "link": "System.Web.UI.HtmlControls.HtmlLink",
    "meta": "System.Web.UI.HtmlControls.HtmlMeta",
    "select": "System.Web.UI.HtmlControls.HtmlSelect",
    "textarea": "System.Web.UI.HtmlControls.HtmlTextArea",
    "button": "System.Web.UI.HtmlControls.HtmlButton",
    "table": "System.Web.UI.HtmlControls.HtmlTable",
}
INPUT_TYPES = {
    "hidden": "System.Web.UI.HtmlControls.HtmlInputHidden",
    "checkbox": "System.Web.UI.HtmlControls.HtmlInputCheckBox",
    "radio": "System.Web.UI.HtmlControls.HtmlInputRadioButton",
    "file": "System.Web.UI.HtmlControls.HtmlInputFile",
    "password": "System.Web.UI.HtmlControls.HtmlInputPassword",
    "submit": "System.Web.UI.HtmlControls.HtmlInputSubmit",
    "button": "System.Web.UI.HtmlControls.HtmlInputButton",
}
ASP_SPECIAL = {"ScriptManager": "System.Web.UI.ScriptManager", "UpdatePanel": "System.Web.UI.UpdatePanel"}

DIRECTIVE = re.compile(r"<%@\s*(Page|Master|Control)\b([^%]*)%>", re.I | re.S)
REGISTER = re.compile(r"<%@\s*Register\b([^%]*)%>", re.I | re.S)
ATTR = re.compile(r'([\w:-]+)\s*=\s*"([^"]*)"', re.S)
CODE_BLOCK = re.compile(r"<%.*?%>", re.S)
COMMENT = re.compile(r"<!--.*?-->|<%--.*?--%>", re.S)
TAG = re.compile(r"<(/?)([A-Za-z][\w:.-]*)((?:\s[^<>]*?)?)(/?)>", re.S)
VOID = {"input", "img", "br", "hr", "meta", "link"}


def attrs(text):
    return {k.lower(): v for k, v in ATTR.findall(text or "")}


def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()


def inherits_of(path):
    m = DIRECTIVE.search(read(path))
    return attrs(m.group(2)).get("inherits") if m else None


def resolve(src, current):
    if src.startswith("~/"):
        return os.path.join(WEB, *src[2:].split("/"))
    return os.path.normpath(os.path.join(os.path.dirname(current), *src.split("/")))


def type_of(name, a, registered):
    lname = name.lower()
    if lname in registered:
        return registered[lname]
    if lname.startswith("asp:"):
        short = name.split(":", 1)[1]
        return ASP_SPECIAL.get(short, "System.Web.UI.WebControls." + short)
    if lname == "input":
        return INPUT_TYPES.get(a.get("type", "text").lower(), "System.Web.UI.HtmlControls.HtmlInputText")
    return HTML_TYPES.get(lname, "System.Web.UI.HtmlControls.HtmlGenericControl")


def controls(path):
    """Server controls with an ID, excluding those inside templates (they are per-item, not fields)."""
    text = read(path)
    registered = {}
    for m in REGISTER.finditer(text):
        a = attrs(m.group(1))
        if {"tagprefix", "tagname", "src"} <= a.keys():
            cls = inherits_of(resolve(a["src"], path))
            if cls:
                registered[(a["tagprefix"] + ":" + a["tagname"]).lower()] = cls

    # Blank out comments and <% %> blocks so '>' inside them cannot break the tag scan.
    text = COMMENT.sub(lambda m: " " * len(m.group(0)), text)
    text = CODE_BLOCK.sub(lambda m: " " * len(m.group(0)), text)

    found, stack, seen = [], [], set()
    for m in TAG.finditer(text):
        closing, name, rest, selfclose = m.groups()
        lname = name.lower()
        if closing:
            while stack and stack.pop() != lname:
                pass
            continue
        a = attrs(rest)
        in_template = any(t.endswith("template") for t in stack)
        if a.get("runat", "").lower() == "server" and "id" in a and not in_template and a["id"] not in seen:
            seen.add(a["id"])
            found.append((a["id"], type_of(name, a, registered)))
        if not selfclose and lname not in VOID:
            stack.append(lname)
    return found


def write_designer(markup):
    cls = inherits_of(markup)
    if not cls:
        return
    ns, _, name = cls.rpartition(".")
    lines = [
        "//------------------------------------------------------------------------------",
        "// <auto-generated>",
        "//     Generated by tools/sync_web_project.py from " + os.path.basename(markup) + ".",
        "//     Do not edit: re-run the tool after changing the markup.",
        "// </auto-generated>",
        "//------------------------------------------------------------------------------",
        "",
        "namespace " + ns,
        "{",
        "    public partial class " + name,
        "    {",
    ] + ["        protected global::%s %s;" % (t, cid) for cid, t in controls(markup)] + ["    }", "}", ""]
    content = "\r\n".join(lines)
    target = markup + ".designer.cs"
    if not os.path.exists(target) or read(target) != content:
        with open(target, "w", encoding="utf-8", newline="") as f:
            f.write(content)


def walk():
    for dirpath, dirnames, filenames in os.walk(WEB):
        dirnames[:] = sorted(d for d in dirnames if d not in EXCLUDED_DIRS and not (dirpath == WEB and d in NOT_SITE))
        for fn in sorted(filenames):
            yield dirpath, fn


def rel(path):
    return os.path.relpath(path, WEB)


def main():
    # Pass 1: designers.
    for dirpath, fn in walk():
        if fn.lower().endswith(MARKUP_EXT):
            write_designer(os.path.join(dirpath, fn))

    # Pass 2: project items.
    compile_items, content_items = [], []
    for dirpath, fn in walk():
        full, low = os.path.join(dirpath, fn), fn.lower()
        if low.endswith(".designer.cs"):
            compile_items.append('    <Compile Include="%s">\r\n      <DependentUpon>%s</DependentUpon>\r\n    </Compile>'
                                 % (rel(full), fn[:-len(".designer.cs")]))
        elif low.endswith(".cs"):
            parent = fn[:-3]
            if parent.lower().endswith(CODE_PARENTS) and os.path.exists(os.path.join(dirpath, parent)):
                compile_items.append('    <Compile Include="%s">\r\n      <DependentUpon>%s</DependentUpon>\r\n      <SubType>ASPXCodeBehind</SubType>\r\n    </Compile>'
                                     % (rel(full), parent))
            else:
                compile_items.append('    <Compile Include="%s" />' % rel(full))
        elif low == "web.config" and dirpath == WEB:
            content_items.append('    <Content Include="Web.config" />')
        elif low in ("web.debug.config", "web.release.config"):
            content_items.append('    <None Include="%s">\r\n      <DependentUpon>Web.config</DependentUpon>\r\n    </None>' % fn)
        elif low.endswith(CONTENT_EXT) or low == "web.config":
            content_items.append('    <Content Include="%s" />' % rel(full))

    block = ("  <!-- BEGIN GENERATED ITEMS (tools/sync_web_project.py) -->\r\n  <ItemGroup>\r\n"
             + "\r\n".join(compile_items) + "\r\n  </ItemGroup>\r\n  <ItemGroup>\r\n"
             + "\r\n".join(content_items) + "\r\n  </ItemGroup>\r\n  <!-- END GENERATED ITEMS -->")
    proj = read(CSPROJ)
    start = proj.index("  <!-- BEGIN GENERATED ITEMS")
    end = proj.index("<!-- END GENERATED ITEMS -->") + len("<!-- END GENERATED ITEMS -->")
    with open(CSPROJ, "w", encoding="utf-8", newline="") as f:
        f.write(proj[:start] + block + proj[end:])
    print("compile items: %d, content items: %d" % (len(compile_items), len(content_items)))


if __name__ == "__main__":
    sys.exit(main())
