using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NPUALibraryCafe.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "books",
                columns: table => new
                {
                    bookid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    author = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    isbn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    bookshelf = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shelfnumber = table.Column<string>(type: "text", nullable: true),
                    physicalcopies = table.Column<int>(type: "integer", nullable: false),
                    availablecopies = table.Column<int>(type: "integer", nullable: false),
                    pdfurl = table.Column<string>(type: "text", nullable: true),
                    pdfavailable = table.Column<bool>(type: "boolean", nullable: false),
                    imagepath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("books_pkey", x => x.bookid);
                });

            migrationBuilder.CreateTable(
                name: "menuitems",
                columns: table => new
                {
                    itemid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    itemname = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    imagepath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("menuitems_pkey", x => x.itemid);
                });

            migrationBuilder.CreateTable(
                name: "systemsettings",
                columns: table => new
                {
                    settingid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    settingname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    settingvalue = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("systemsettings_pkey", x => x.settingid);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    userid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fullname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    passwordhash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.userid);
                });

            migrationBuilder.CreateTable(
                name: "bookreviews",
                columns: table => new
                {
                    reviewid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    bookid = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("bookreviews_pkey", x => x.reviewid);
                    table.ForeignKey(
                        name: "bookreviews_bookid_fkey",
                        column: x => x.bookid,
                        principalTable: "books",
                        principalColumn: "bookid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "bookreviews_userid_fkey",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "borrowings",
                columns: table => new
                {
                    borrowid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    bookid = table.Column<int>(type: "integer", nullable: false),
                    borrowdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    duedate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    returndate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("borrowings_pkey", x => x.borrowid);
                    table.ForeignKey(
                        name: "borrowings_bookid_fkey",
                        column: x => x.bookid,
                        principalTable: "books",
                        principalColumn: "bookid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "borrowings_userid_fkey",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cafeorders",
                columns: table => new
                {
                    orderid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    orderdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    totalamount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ordertype = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'Pending'::character varying"),
                    notifiedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    confirmedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    completedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cafeorders_pkey", x => x.orderid);
                    table.ForeignKey(
                        name: "cafeorders_userid_fkey",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cafereviews",
                columns: table => new
                {
                    reviewid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    itemid = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cafereviews_pkey", x => x.reviewid);
                    table.ForeignKey(
                        name: "cafereviews_itemid_fkey",
                        column: x => x.itemid,
                        principalTable: "menuitems",
                        principalColumn: "itemid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "cafereviews_userid_fkey",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    notificationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    isread = table.Column<bool>(type: "boolean", nullable: false),
                    relatedid = table.Column<int>(type: "integer", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.notificationid);
                    table.ForeignKey(
                        name: "FK_notifications_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    reservationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    reservationtype = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    starttime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    endtime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notificationsentat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    confirmedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancelledat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservations", x => x.reservationid);
                    table.ForeignKey(
                        name: "FK_reservations_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cafeorderitems",
                columns: table => new
                {
                    orderitemid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderid = table.Column<int>(type: "integer", nullable: false),
                    itemid = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cafeorderitems_pkey", x => x.orderitemid);
                    table.ForeignKey(
                        name: "cafeorderitems_itemid_fkey",
                        column: x => x.itemid,
                        principalTable: "menuitems",
                        principalColumn: "itemid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "cafeorderitems_orderid_fkey",
                        column: x => x.orderid,
                        principalTable: "cafeorders",
                        principalColumn: "orderid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    paymentid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    paymentmethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    paymentdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("payments_pkey", x => x.paymentid);
                    table.ForeignKey(
                        name: "payments_orderid_fkey",
                        column: x => x.orderid,
                        principalTable: "cafeorders",
                        principalColumn: "orderid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "payments_userid_fkey",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservationseats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reservationid = table.Column<int>(type: "integer", nullable: false),
                    seatid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservationseats", x => x.id);
                    table.ForeignKey(
                        name: "FK_reservationseats_reservations_reservationid",
                        column: x => x.reservationid,
                        principalTable: "reservations",
                        principalColumn: "reservationid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookreviews_bookid",
                table: "bookreviews",
                column: "bookid");

            migrationBuilder.CreateIndex(
                name: "IX_bookreviews_userid",
                table: "bookreviews",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "books_isbn_key",
                table: "books",
                column: "isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_borrowings_bookid",
                table: "borrowings",
                column: "bookid");

            migrationBuilder.CreateIndex(
                name: "IX_borrowings_userid",
                table: "borrowings",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_cafeorderitems_itemid",
                table: "cafeorderitems",
                column: "itemid");

            migrationBuilder.CreateIndex(
                name: "IX_cafeorderitems_orderid",
                table: "cafeorderitems",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_cafeorders_userid",
                table: "cafeorders",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_cafereviews_itemid",
                table: "cafereviews",
                column: "itemid");

            migrationBuilder.CreateIndex(
                name: "IX_cafereviews_userid",
                table: "cafereviews",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_userid",
                table: "notifications",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_payments_orderid",
                table: "payments",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_payments_userid",
                table: "payments",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_userid",
                table: "reservations",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_reservationseats_reservationid",
                table: "reservationseats",
                column: "reservationid");

            migrationBuilder.CreateIndex(
                name: "systemsettings_settingname_key",
                table: "systemsettings",
                column: "settingname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookreviews");

            migrationBuilder.DropTable(
                name: "borrowings");

            migrationBuilder.DropTable(
                name: "cafeorderitems");

            migrationBuilder.DropTable(
                name: "cafereviews");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "reservationseats");

            migrationBuilder.DropTable(
                name: "systemsettings");

            migrationBuilder.DropTable(
                name: "books");

            migrationBuilder.DropTable(
                name: "menuitems");

            migrationBuilder.DropTable(
                name: "cafeorders");

            migrationBuilder.DropTable(
                name: "reservations");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
