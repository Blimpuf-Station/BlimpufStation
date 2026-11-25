using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database._Blimpuf.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class BlimpufSecondaryPronouns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blimpuf_profile",
                columns: table => new
                {
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    secondary_gender = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blimpuf_profile", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_blimpuf_profile_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blimpuf_profile");
        }
    }
}
