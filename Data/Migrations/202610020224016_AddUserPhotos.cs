namespace StudyLink.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserPhotos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserPhotos",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        Photo = c.Binary(nullable: false),
                        ContentType = c.String(nullable: false, maxLength: 50),
                        UpdatedAt = c.DateTime(nullable: false, precision: 0, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserPhotos", "UserId", "dbo.Users");
            DropIndex("dbo.UserPhotos", new[] { "UserId" });
            DropTable("dbo.UserPhotos");
        }
    }
}
