using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AUDIT_LOG_EVENT",
                columns: table => new
                {
                    AuditLogEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_LOG_EVENT", x => x.AuditLogEventId);
                });

            migrationBuilder.CreateTable(
                name: "AUDIT_LOG_EVENT_CHANGE",
                columns: table => new
                {
                    AuditLogEventChangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditLogEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_LOG_EVENT_CHANGE", x => x.AuditLogEventChangeId);
                    table.ForeignKey(
                        name: "FK_AUDIT_LOG_EVENT_CHANGE_AUDIT_LOG_EVENT_AuditLogEventId",
                        column: x => x.AuditLogEventId,
                        principalTable: "AUDIT_LOG_EVENT",
                        principalColumn: "AuditLogEventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOG_EVENT_PlateId",
                table: "AUDIT_LOG_EVENT",
                column: "PlateId");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOG_EVENT_PlateId_Timestamp",
                table: "AUDIT_LOG_EVENT",
                columns: new[] { "PlateId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOG_EVENT_Timestamp",
                table: "AUDIT_LOG_EVENT",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOG_EVENT_CHANGE_AuditLogEventId",
                table: "AUDIT_LOG_EVENT_CHANGE",
                column: "AuditLogEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AUDIT_LOG_EVENT_CHANGE");

            migrationBuilder.DropTable(
                name: "AUDIT_LOG_EVENT");
        }
    }
}
