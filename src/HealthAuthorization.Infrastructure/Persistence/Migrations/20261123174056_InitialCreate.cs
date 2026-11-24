using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HealthAuthorization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Identification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contractors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intermediaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Identification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intermediaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Affiliates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AffiliateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Identification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ContractorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Affiliates", x => x.Id);
                    table.CheckConstraint("CK_Affiliates_Document", "([Identification] IS NULL AND [DocumentType] = 0) OR ([Identification] IS NOT NULL AND LEN(LTRIM(RTRIM([Identification]))) > 0 AND [DocumentType] IN (1,2,3))");
                    table.ForeignKey(
                        name: "FK_Affiliates_Contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Contractors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Identification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Providers_ProviderTypes_ProviderTypeId",
                        column: x => x.ProviderTypeId,
                        principalTable: "ProviderTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Authorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffiliateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IntermediaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Authorizations_Affiliates_AffiliateId",
                        column: x => x.AffiliateId,
                        principalTable: "Affiliates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Authorizations_Contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Contractors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Authorizations_Intermediaries_IntermediaryId",
                        column: x => x.IntermediaryId,
                        principalTable: "Intermediaries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimedAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    ArsAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    AffiliateAmount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationDetails", x => x.Id);
                    table.CheckConstraint("CK_AuthorizationDetails_Amounts", "[ClaimedAmount] >= 0 AND [ArsAmount] >= 0 AND [AffiliateAmount] >= 0 AND [ClaimedAmount] = [ArsAmount] + [AffiliateAmount]");
                    table.ForeignKey(
                        name: "FK_AuthorizationDetails_Authorizations_AuthorizationId",
                        column: x => x.AuthorizationId,
                        principalTable: "Authorizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuthorizationDetails_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuthorizationDetails_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Contractors",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Identification", "IsActive", "LastUpdatedBy", "LastUpdatedDate", "Name" },
                values: new object[] { new Guid("000000c9-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DEMO-CONTRACTOR", true, null, null, "Contratante de demostración" });

            migrationBuilder.InsertData(
                table: "Intermediaries",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Identification", "IsActive", "LastUpdatedBy", "LastUpdatedDate", "Name" },
                values: new object[] { new Guid("000000ca-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DEMO-INTERMEDIARY", true, null, null, "Intermediario de demostración" });

            migrationBuilder.InsertData(
                table: "ProviderTypes",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Description", "IsActive", "LastUpdatedBy", "LastUpdatedDate", "Name" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, null, null, "Médico" },
                    { new Guid("00000002-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, null, null, "Farmacia" },
                    { new Guid("00000003-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, null, null, "Clínica" },
                    { new Guid("00000004-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, null, null, "Laboratorio" }
                });

            migrationBuilder.InsertData(
                table: "ServiceTypes",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Description", "IsActive", "LastUpdatedBy", "LastUpdatedDate", "Name" },
                values: new object[,]
                {
                    { new Guid("0000000b-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, null, null, "Consulta" },
                    { new Guid("0000000c-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, null, null, "Emergencia" },
                    { new Guid("0000000d-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, null, null, "Hospitalización" },
                    { new Guid("0000000e-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, null, null, "Laboratorio" },
                    { new Guid("0000000f-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, null, null, "Imágenes" }
                });

            migrationBuilder.InsertData(
                table: "Affiliates",
                columns: new[] { "Id", "AffiliateNumber", "BirthDate", "ContractorId", "CreatedBy", "CreatedDate", "DocumentType", "FirstName", "Identification", "IsActive", "LastName", "LastUpdatedBy", "LastUpdatedDate" },
                values: new object[] { new Guid("000000cb-0000-0000-0000-000000000000"), "DEMO-001", new DateOnly(1990, 1, 1), new Guid("000000c9-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0, "Afiliado", null, true, "Demostración", null, null });

            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Identification", "IsActive", "LastUpdatedBy", "LastUpdatedDate", "Name", "ProviderTypeId" },
                values: new object[,]
                {
                    { new Guid("00000065-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DEMO-PROVIDER-1", true, null, null, "Prestador de demostración (Médico)", new Guid("00000001-0000-0000-0000-000000000000") },
                    { new Guid("00000066-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DEMO-PROVIDER-2", true, null, null, "Prestador de demostración (Farmacia)", new Guid("00000002-0000-0000-0000-000000000000") },
                    { new Guid("00000067-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DEMO-PROVIDER-3", true, null, null, "Prestador de demostración (Clínica)", new Guid("00000003-0000-0000-0000-000000000000") },
                    { new Guid("00000068-0000-0000-0000-000000000000"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DEMO-PROVIDER-4", true, null, null, "Prestador de demostración (Laboratorio)", new Guid("00000004-0000-0000-0000-000000000000") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Affiliates_AffiliateNumber",
                table: "Affiliates",
                column: "AffiliateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Affiliates_ContractorId",
                table: "Affiliates",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Affiliates_Identification",
                table: "Affiliates",
                column: "Identification",
                unique: true,
                filter: "[Identification] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationDetails_AuthorizationId",
                table: "AuthorizationDetails",
                column: "AuthorizationId",
                filter: "[IsDelete] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationDetails_ProviderId",
                table: "AuthorizationDetails",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationDetails_ServiceTypeId",
                table: "AuthorizationDetails",
                column: "ServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Authorizations_AffiliateId_CreatedDate_Id",
                table: "Authorizations",
                columns: new[] { "AffiliateId", "CreatedDate", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Authorizations_ContractorId",
                table: "Authorizations",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Authorizations_CreatedDate_Id",
                table: "Authorizations",
                columns: new[] { "CreatedDate", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Authorizations_IntermediaryId",
                table: "Authorizations",
                column: "IntermediaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Authorizations_IsActive_CreatedDate_Id",
                table: "Authorizations",
                columns: new[] { "IsActive", "CreatedDate", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_ProviderTypeId",
                table: "Providers",
                column: "ProviderTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizationDetails");

            migrationBuilder.DropTable(
                name: "Authorizations");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "ServiceTypes");

            migrationBuilder.DropTable(
                name: "Affiliates");

            migrationBuilder.DropTable(
                name: "Intermediaries");

            migrationBuilder.DropTable(
                name: "ProviderTypes");

            migrationBuilder.DropTable(
                name: "Contractors");
        }
    }
}
