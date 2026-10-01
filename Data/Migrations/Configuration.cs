using System.Data.Entity.Migrations;
using StudyLink.Data.Seed;

namespace StudyLink.Data.Migrations
{
    /// <summary>
    /// Schema changes are explicit, reviewed migrations only - never automatic. The same
    /// migrations produce database\01_CreateSchema.sql for installs done by script.
    /// </summary>
    public sealed class Configuration : DbMigrationsConfiguration<StudyLinkDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            AutomaticMigrationDataLossAllowed = false;
            ContextKey = "StudyLink";
        }

        protected override void Seed(StudyLinkDbContext context)
        {
            LookupData.Apply(context);
        }
    }
}
