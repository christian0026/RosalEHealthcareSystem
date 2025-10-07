namespace RosalEHealthcare.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Medicines",
                c => new
                    {
                        MedicineId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Category = c.String(maxLength: 50),
                        Stock = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ExpiryDate = c.DateTime(nullable: false),
                        Status = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.MedicineId);
            
            CreateTable(
                "dbo.Patients",
                c => new
                    {
                        PatientId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Gender = c.String(maxLength: 10),
                        BirthDate = c.DateTime(nullable: false),
                        ContactNumber = c.String(maxLength: 15),
                        Condition = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.PatientId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 100),
                        PasswordHash = c.String(nullable: false, maxLength: 255),
                        Role = c.String(nullable: false, maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Users");
            DropTable("dbo.Patients");
            DropTable("dbo.Medicines");
        }
    }
}
