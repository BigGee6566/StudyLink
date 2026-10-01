using System;
using StudyLink.Web.Infrastructure;

namespace StudyLink.Web.Public
{
    public partial class Privacy : CataloguePage
    {
        protected void Page_Load(object sender, EventArgs e) => Subheading = "How StudyLink uses your information.";
    }
}