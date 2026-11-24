using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthAuthorization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "Authorizations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "IdempotencyRequestHash",
                table: "Authorizations",
                type: "binary(32)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyScopeId",
                table: "Authorizations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Authorizations_IdempotencyScope_Key",
                table: "Authorizations",
                columns: new[] { "IdempotencyScopeId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Authorizations_Idempotency",
                table: "Authorizations",
                sql: "([IdempotencyScopeId] IS NULL AND [IdempotencyKey] IS NULL AND [IdempotencyRequestHash] IS NULL) OR ([IdempotencyScopeId] IS NOT NULL AND [IdempotencyKey] IS NOT NULL AND [IdempotencyRequestHash] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Authorizations_IdempotencyScope_Key",
                table: "Authorizations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Authorizations_Idempotency",
                table: "Authorizations");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Authorizations");

            migrationBuilder.DropColumn(
                name: "IdempotencyRequestHash",
                table: "Authorizations");

            migrationBuilder.DropColumn(
                name: "IdempotencyScopeId",
                table: "Authorizations");
        }
    }
}
