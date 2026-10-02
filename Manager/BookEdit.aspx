<%@ Page Title="Book" Language="C#" MasterPageFile="~/MasterPages/App.Master" AutoEventWireup="true" CodeBehind="BookEdit.aspx.cs" Inherits="StudyLink.Web.Manager.BookEdit" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <p class="mb-3"><a href="<%: ResolveUrl("~/Manager/Books.aspx") %>">&larr; Book Management</a></p>
    <div class="detail-grid">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Title details</h2><asp:Literal ID="StatusBadge" runat="server" /></div>
            <div class="panel__body">
                <div class="row g-3">
                    <div class="col-md-8"><label class="form-label" for="<%= BookTitle.ClientID %>">Title</label><asp:TextBox ID="BookTitle" runat="server" CssClass="form-control" MaxLength="200" /></div>
                    <div class="col-md-4"><label class="form-label" for="<%= Isbn.ClientID %>">ISBN</label><asp:TextBox ID="Isbn" runat="server" CssClass="form-control" MaxLength="17" /></div>
                    <div class="col-md-8"><label class="form-label" for="<%= Authors.ClientID %>">Author(s)</label><asp:TextBox ID="Authors" runat="server" CssClass="form-control" MaxLength="200" /></div>
                    <div class="col-md-4"><label class="form-label" for="<%= Edition.ClientID %>">Edition</label><asp:TextBox ID="Edition" runat="server" CssClass="form-control" MaxLength="40" /></div>
                    <div class="col-md-8"><label class="form-label" for="<%= Publisher.ClientID %>">Publisher</label><asp:TextBox ID="Publisher" runat="server" CssClass="form-control" MaxLength="120" /></div>
                    <div class="col-md-4"><label class="form-label" for="<%= Year.ClientID %>">Year</label><asp:TextBox ID="Year" runat="server" CssClass="form-control" TextMode="Number" /></div>
                    <div class="col-md-4"><label class="form-label" for="<%= Category.ClientID %>">Category</label><asp:DropDownList ID="Category" runat="server" CssClass="form-select" /></div>
                    <div class="col-md-4"><label class="form-label" for="<%= Subject.ClientID %>">Subject</label><asp:DropDownList ID="Subject" runat="server" CssClass="form-select" /></div>
                    <div class="col-md-4"><label class="form-label" for="<%= Supplier.ClientID %>">Preferred supplier</label><asp:DropDownList ID="Supplier" runat="server" CssClass="form-select" /></div>
                    <div class="col-md-3"><label class="form-label" for="<%= SalePrice.ClientID %>">Sale price (R)</label><asp:TextBox ID="SalePrice" runat="server" CssClass="form-control" inputmode="decimal" /></div>
                    <div class="col-md-3"><label class="form-label" for="<%= WeeklyFee.ClientID %>">Rental / week (R)</label><asp:TextBox ID="WeeklyFee" runat="server" CssClass="form-control" inputmode="decimal" /></div>
                    <div class="col-md-3"><label class="form-label" for="<%= LateFee.ClientID %>">Late fee / day (R)</label><asp:TextBox ID="LateFee" runat="server" CssClass="form-control" inputmode="decimal" /></div>
                    <div class="col-md-3"><label class="form-label" for="<%= Reorder.ClientID %>">Reorder level</label><asp:TextBox ID="Reorder" runat="server" CssClass="form-control" TextMode="Number" /></div>
                    <div class="col-12"><label class="form-label" for="<%= Description.ClientID %>">Description</label><asp:TextBox ID="Description" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" MaxLength="2000" /></div>
                    <div class="col-md-6">
                        <label class="form-label" for="<%= Cover.ClientID %>">Cover image<%= QueryInt("id").HasValue ? "" : " *" %></label>
                        <div class="d-flex gap-3 align-items-start">
                            <asp:Literal ID="CoverPreview" runat="server" />
                            <div class="flex-fill">
                                <asp:FileUpload ID="Cover" runat="server" CssClass="form-control" accept="image/jpeg,image/png,image/webp" />
                                <div class="form-hint">A clear photo of the front cover (JPG, PNG or WebP, 2 MB max). <asp:Literal ID="CoverHint" runat="server" Text="Required for new titles, so students can check they are choosing the right edition." /></div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6 d-flex align-items-end gap-3">
                        <div class="form-check"><asp:CheckBox ID="Rentable" runat="server" /><label class="form-check-label" for="<%= Rentable.ClientID %>">Can be rented</label></div>
                        <div class="form-check"><asp:CheckBox ID="ForSale" runat="server" /><label class="form-check-label" for="<%= ForSale.ClientID %>">Can be bought</label></div>
                    </div>
                </div>
                <div class="form-actions">
                    <asp:Button ID="SaveButton" runat="server" CssClass="btn btn-primary" Text="Save" OnClick="Save_Click" />
                    <asp:Button ID="RetireButton" runat="server" CssClass="btn btn-outline-danger" Text="Retire title" OnClick="Retire_Click" data-confirm="Retire this title? It disappears from the catalogue; existing rentals are unaffected." />
                    <asp:Button ID="RestoreButton" runat="server" CssClass="btn btn-outline-primary" Text="Restore title" OnClick="Restore_Click" />
                </div>
            </div>
        </div>

        <asp:PlaceHolder ID="CopiesSide" runat="server">
            <div>
                <div class="panel">
                    <div class="panel__header"><h2 class="panel__title">Add copies</h2></div>
                    <div class="panel__body">
                        <div class="row g-2">
                            <div class="col-6"><label class="form-label" for="<%= CopyCampus.ClientID %>">Campus</label><asp:DropDownList ID="CopyCampus" runat="server" CssClass="form-select" /></div>
                            <div class="col-6"><label class="form-label" for="<%= CopyQty.ClientID %>">Quantity</label><asp:TextBox ID="CopyQty" runat="server" CssClass="form-control" TextMode="Number" Text="1" /></div>
                            <div class="col-6"><label class="form-label" for="<%= CopyCondition.ClientID %>">Condition</label><asp:DropDownList ID="CopyCondition" runat="server" CssClass="form-select" /></div>
                            <div class="col-6"><label class="form-label" for="<%= CopyNotes.ClientID %>">Notes</label><asp:TextBox ID="CopyNotes" runat="server" CssClass="form-control" MaxLength="200" /></div>
                        </div>
                        <asp:Button ID="AddCopiesButton" runat="server" CssClass="btn btn-outline-primary mt-3" Text="Add copies" OnClick="AddCopies_Click" />
                        <p class="form-hint mt-2">Stock normally arrives through supplier deliveries; use this for donations or stock counts.</p>
                    </div>
                </div>
                <asp:PlaceHolder ID="CopyEdit" runat="server" Visible="false">
                    <div class="panel">
                        <div class="panel__header"><h2 class="panel__title">Copy <asp:Literal ID="CopyBarcode" runat="server" /></h2></div>
                        <div class="panel__body">
                            <div class="row g-2">
                                <div class="col-6"><label class="form-label" for="<%= EditStatus.ClientID %>">Status</label><asp:DropDownList ID="EditStatus" runat="server" CssClass="form-select" /></div>
                                <div class="col-6"><label class="form-label" for="<%= EditCondition.ClientID %>">Condition</label><asp:DropDownList ID="EditCondition" runat="server" CssClass="form-select" /></div>
                                <div class="col-12"><label class="form-label" for="<%= EditNotes.ClientID %>">Notes</label><asp:TextBox ID="EditNotes" runat="server" CssClass="form-control" MaxLength="200" /></div>
                            </div>
                            <asp:Button ID="UpdateCopyButton" runat="server" CssClass="btn btn-primary mt-3" Text="Update copy" OnClick="UpdateCopy_Click" />
                        </div>
                    </div>
                </asp:PlaceHolder>
            </div>
        </asp:PlaceHolder>
    </div>

    <asp:PlaceHolder ID="CopiesPanel" runat="server">
        <div class="panel">
            <div class="panel__header"><h2 class="panel__title">Copies</h2><span class="muted small"><asp:Literal ID="CopyCount" runat="server" /></span></div>
            <div class="table-wrap">
                <table class="data-table stack">
                    <thead><tr><th>Barcode</th><th>Campus</th><th>Condition</th><th>Status</th><th>With</th><th>Source</th><th class="col-action"></th></tr></thead>
                    <tbody>
                        <asp:Repeater ID="Copies" runat="server">
                            <ItemTemplate>
                                <tr>
                                    <td data-label="Barcode" class="numeric"><%#: Eval("Barcode") %></td>
                                    <td data-label="Campus"><%#: Eval("Campus") %></td>
                                    <td data-label="Condition"><%#: Fmt.Words(Eval("Condition")) %></td>
                                    <td data-label="Status"><%# Fmt.Badge(Eval("Status")) %></td>
                                    <td data-label="With"><%#: Eval("Holder") %><%# Eval("DueDate") == null ? "" : "<div class=\"muted small\">due " + Fmt.Date((DateTime?)Eval("DueDate")) + "</div>" %></td>
                                    <td data-label="Source" class="small"><%#: Eval("Source") %></td>
                                    <td class="col-action"><a class="btn btn-sm btn-outline-primary" href="<%# ResolveUrl("~/Manager/BookEdit.aspx?id=" + BookIdValue + "&copy=" + Eval("CopyId")) %>">Edit</a></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
            </div>
        </div>
    </asp:PlaceHolder>
</asp:Content>
