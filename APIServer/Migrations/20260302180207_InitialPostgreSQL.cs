using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Client",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    contactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    companyName = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    city = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    zipcode = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    country = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    logo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    logoThumbnail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    revenueCatCustomerId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    subscriptionActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    subscriptionPlan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    subscriptionUserPacks = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    subscriptionExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    subscriptionLastVerified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyConfig",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    defaultTermsAndConditions = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    lunchDeductionMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    workDayStartHour = table.Column<int>(type: "integer", nullable: false, defaultValue: 7),
                    workDayStartMinute = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    workDayEndHour = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    workDayEndMinute = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    workDays = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "1,2,3,4,5"),
                    enableClockInReminder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    enableClockOutReminder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    reminderMinutesBefore = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    requireApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    overtimeThresholdHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 8m),
                    weeklyOvertimeThresholdHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 40m),
                    enableAutoClockIn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    autoClockInRadiusMeters = table.Column<double>(type: "double precision", nullable: false, defaultValue: 100.0),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyConfig", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "EstimateTemplate",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimateTemplate", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCategory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCategory", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    productId = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "QuickStartTemplates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    displayOrder = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickStartTemplates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    s3Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    fileName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size = table.Column<double>(type: "double precision", nullable: false),
                    contentType = table.Column<string>(type: "text", nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    fullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    phone = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    city = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    zipcode = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    country = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    verificationToken = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changePasswordRequired = table.Column<bool>(type: "boolean", nullable: false),
                    personalPhoto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    personalPhotoThumbnail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    passwordResetToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    passwordResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inviteToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    inviteTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    googleAccessToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    googleRefreshToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    googleTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    googleCalendarLinked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    googleEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    squareAccessToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    squareRefreshToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    squareTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    squareLinked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    squareMerchantId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    squareLocationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    addressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    addressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    zipcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    clientId = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    addressLine1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    addressLine2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    zipcode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    isPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.id);
                    table.ForeignKey(
                        name: "FK_Location_Client_clientId",
                        column: x => x.clientId,
                        principalTable: "Client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientNotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    clientId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    createdBy = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientNotes", x => x.id);
                    table.ForeignKey(
                        name: "FK_ClientNotes_Client_clientId",
                        column: x => x.clientId,
                        principalTable: "Client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientNotes_User_createdBy",
                        column: x => x.createdBy,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    platform = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    deviceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_DeviceTokens_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceTokens_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    materialReturn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    timecardReminder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    jobAssignment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    rewardGoal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    reviewPrompt = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCompany",
                columns: table => new
                {
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assignedDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompany", x => new { x.userId, x.companyId });
                    table.ForeignKey(
                        name: "FK_UserCompany_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCompany_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    permissionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    granted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    grantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    grantedDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revokedDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_UserPermissions_User_grantedByUserId",
                        column: x => x.grantedByUserId,
                        principalTable: "User",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_UserPermissions_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    productId = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    imageS3Key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory", x => x.id);
                    table.ForeignKey(
                        name: "FK_Inventory_Warehouse_warehouseId",
                        column: x => x.warehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Estimates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    clientid = table.Column<Guid>(type: "uuid", nullable: true),
                    locationid = table.Column<Guid>(type: "uuid", nullable: true),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sent = table.Column<bool>(type: "boolean", nullable: false),
                    approved = table.Column<bool>(type: "boolean", nullable: false),
                    sentDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    proposedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    termsAndConditions = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    publicShareId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    viewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    lastViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estimates", x => x.id);
                    table.ForeignKey(
                        name: "FK_Estimates_Client_clientid",
                        column: x => x.clientid,
                        principalTable: "Client",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Estimates_Location_locationid",
                        column: x => x.locationid,
                        principalTable: "Location",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryBatch",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    inventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    quantityAdded = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantityRemaining = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    addedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isExpired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBatch", x => x.id);
                    table.ForeignKey(
                        name: "FK_InventoryBatch_Inventory_inventoryId",
                        column: x => x.inventoryId,
                        principalTable: "Inventory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstimateAlbums",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    estimateId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    orderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimateAlbums", x => x.id);
                    table.ForeignKey(
                        name: "FK_EstimateAlbums_Estimates_estimateId",
                        column: x => x.estimateId,
                        principalTable: "Estimates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EstimateItem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    estimateId = table.Column<Guid>(type: "uuid", nullable: true),
                    estimateTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    productId = table.Column<Guid>(type: "uuid", nullable: true),
                    warehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    inventoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    labor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimateItem", x => x.id);
                    table.ForeignKey(
                        name: "FK_EstimateItem_EstimateTemplate_estimateTemplateId",
                        column: x => x.estimateTemplateId,
                        principalTable: "EstimateTemplate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstimateItem_Estimates_estimateId",
                        column: x => x.estimateId,
                        principalTable: "Estimates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    jobNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    estimateId = table.Column<Guid>(type: "uuid", nullable: true),
                    scheduledStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scheduledEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estimatedTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    laborCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    materialsCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    clientid = table.Column<Guid>(type: "uuid", nullable: true),
                    locationid = table.Column<Guid>(type: "uuid", nullable: true),
                    isRecurring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    recurrenceRule = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    recurrenceEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    timeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lastOccurrenceCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    billingFrequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    parentJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    occurrenceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    googleCalendarEventId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.id);
                    table.ForeignKey(
                        name: "FK_Job_Client_clientid",
                        column: x => x.clientid,
                        principalTable: "Client",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Job_Estimates_estimateId",
                        column: x => x.estimateId,
                        principalTable: "Estimates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Job_Job_parentJobId",
                        column: x => x.parentJobId,
                        principalTable: "Job",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Job_Location_locationid",
                        column: x => x.locationid,
                        principalTable: "Location",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "EstimateMedia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    estimateId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    albumId = table.Column<Guid>(type: "uuid", nullable: true),
                    estimateItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    webUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    thumbUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    orderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimateMedia", x => x.id);
                    table.ForeignKey(
                        name: "FK_EstimateMedia_EstimateAlbums_albumId",
                        column: x => x.albumId,
                        principalTable: "EstimateAlbums",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EstimateMedia_EstimateItem_estimateItemId",
                        column: x => x.estimateItemId,
                        principalTable: "EstimateItem",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EstimateMedia_Estimates_estimateId",
                        column: x => x.estimateId,
                        principalTable: "Estimates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryUsageHistory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    inventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    jobId = table.Column<Guid>(type: "uuid", nullable: true),
                    actionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    previousQuantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    newQuantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    performedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryUsageHistory", x => x.id);
                    table.ForeignKey(
                        name: "FK_InventoryUsageHistory_Inventory_inventoryId",
                        column: x => x.inventoryId,
                        principalTable: "Inventory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryUsageHistory_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryUsageHistory_User_performedByUserId",
                        column: x => x.performedByUserId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "JobAlbums",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    orderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAlbums", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobAlbums_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobAssignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    assignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    assignedDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unassignedDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAssignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobAssignments_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_JobAssignments_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobAssignments_User_assignedByUserId",
                        column: x => x.assignedByUserId,
                        principalTable: "User",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_JobAssignments_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "JobGoals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    bonusAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    completionBonus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    targetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    createdByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    completedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    completedOnTime = table.Column<bool>(type: "boolean", nullable: true),
                    requiresVerification = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    verifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobGoals", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobGoals_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobGoals_User_assignedToUserId",
                        column: x => x.assignedToUserId,
                        principalTable: "User",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_JobGoals_User_completedBy",
                        column: x => x.completedBy,
                        principalTable: "User",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_JobGoals_User_createdByUserId",
                        column: x => x.createdByUserId,
                        principalTable: "User",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_JobGoals_User_verifiedBy",
                        column: x => x.verifiedBy,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "JobInventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    jobId = table.Column<Guid>(type: "uuid", nullable: false),
                    productId = table.Column<Guid>(type: "uuid", nullable: true),
                    warehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    inventoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    idealQuantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    actualQuantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    labor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobInventory", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobInventory_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobInventory_Warehouse_warehouseId",
                        column: x => x.warehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "JobMilestones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    completedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    orderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobMilestones", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobMilestones_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobMilestones_User_completedBy",
                        column: x => x.completedBy,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "JobNotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    createdBy = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobNotes", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobNotes_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobNotes_User_createdBy",
                        column: x => x.createdBy,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    jobId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    squareTransactionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    squarePaymentId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    squarePaymentLinkId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    squarePaymentLinkUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    squareOrderId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    paymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cardBrand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lastFourDigits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    failureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completedDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_Payments_Company_companyId",
                        column: x => x.companyId,
                        principalTable: "Company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeCards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    jobId = table.Column<Guid>(type: "uuid", nullable: true),
                    clockInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    clockOutTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    regularHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    overtimeHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    totalHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    breakMinutes = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    billableHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    approvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    approvedDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    isAutoClockIn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    isAutoClockOut = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    clockInLatitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: true),
                    clockInLongitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: true),
                    clockOutLatitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: true),
                    clockOutLongitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: true),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeCards", x => x.id);
                    table.ForeignKey(
                        name: "FK_TimeCards_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimeCards_User_approvedByUserId",
                        column: x => x.approvedByUserId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimeCards_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobGoalMedia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    goalId = table.Column<Guid>(type: "uuid", nullable: false),
                    webUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    thumbUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobGoalMedia", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobGoalMedia_JobGoals_goalId",
                        column: x => x.goalId,
                        principalTable: "JobGoals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobGoalVotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    goalId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    voteComplete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    voteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobGoalVotes", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobGoalVotes_JobGoals_goalId",
                        column: x => x.goalId,
                        principalTable: "JobGoals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobGoalVotes_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "JobMedia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobId = table.Column<Guid>(type: "uuid", nullable: false),
                    companyId = table.Column<Guid>(type: "uuid", nullable: false),
                    albumId = table.Column<Guid>(type: "uuid", nullable: true),
                    jobInventoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    webUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    thumbUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    orderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    createDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobMedia", x => x.id);
                    table.ForeignKey(
                        name: "FK_JobMedia_JobAlbums_albumId",
                        column: x => x.albumId,
                        principalTable: "JobAlbums",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JobMedia_JobInventory_jobInventoryId",
                        column: x => x.jobInventoryId,
                        principalTable: "JobInventory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JobMedia_Job_jobId",
                        column: x => x.jobId,
                        principalTable: "Job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientNotes_clientId",
                table: "ClientNotes",
                column: "clientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientNotes_createdBy",
                table: "ClientNotes",
                column: "createdBy");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyConfig_companyId",
                table: "CompanyConfig",
                column: "companyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_companyId",
                table: "DeviceTokens",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_userId_token",
                table: "DeviceTokens",
                columns: new[] { "userId", "token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstimateAlbums_estimateId",
                table: "EstimateAlbums",
                column: "estimateId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimateItem_estimateId",
                table: "EstimateItem",
                column: "estimateId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimateItem_estimateTemplateId",
                table: "EstimateItem",
                column: "estimateTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimateMedia_albumId",
                table: "EstimateMedia",
                column: "albumId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimateMedia_estimateId",
                table: "EstimateMedia",
                column: "estimateId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimateMedia_estimateItemId",
                table: "EstimateMedia",
                column: "estimateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_clientid",
                table: "Estimates",
                column: "clientid");

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_locationid",
                table: "Estimates",
                column: "locationid");

            migrationBuilder.CreateIndex(
                name: "IX_Estimates_publicShareId",
                table: "Estimates",
                column: "publicShareId",
                unique: true,
                filter: "\"publicShareId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_warehouseId",
                table: "Inventory",
                column: "warehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatch_companyId_expiresAt",
                table: "InventoryBatch",
                columns: new[] { "companyId", "expiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatch_expiresAt",
                table: "InventoryBatch",
                column: "expiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatch_inventoryId",
                table: "InventoryBatch",
                column: "inventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategory_companyId_name",
                table: "InventoryCategory",
                columns: new[] { "companyId", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryUsageHistory_inventoryId",
                table: "InventoryUsageHistory",
                column: "inventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryUsageHistory_jobId",
                table: "InventoryUsageHistory",
                column: "jobId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryUsageHistory_performedByUserId",
                table: "InventoryUsageHistory",
                column: "performedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_clientid",
                table: "Job",
                column: "clientid");

            migrationBuilder.CreateIndex(
                name: "IX_Job_estimateId",
                table: "Job",
                column: "estimateId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_locationid",
                table: "Job",
                column: "locationid");

            migrationBuilder.CreateIndex(
                name: "IX_Job_parentJobId",
                table: "Job",
                column: "parentJobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAlbums_jobId",
                table: "JobAlbums",
                column: "jobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_assignedByUserId",
                table: "JobAssignments",
                column: "assignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_companyId",
                table: "JobAssignments",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_jobId_userId_active",
                table: "JobAssignments",
                columns: new[] { "jobId", "userId", "active" });

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_userId_active",
                table: "JobAssignments",
                columns: new[] { "userId", "active" });

            migrationBuilder.CreateIndex(
                name: "IX_JobGoalMedia_goalId",
                table: "JobGoalMedia",
                column: "goalId");

            migrationBuilder.CreateIndex(
                name: "IX_JobGoals_assignedToUserId_status",
                table: "JobGoals",
                columns: new[] { "assignedToUserId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobGoals_companyId_status",
                table: "JobGoals",
                columns: new[] { "companyId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobGoals_completedBy",
                table: "JobGoals",
                column: "completedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JobGoals_createdByUserId",
                table: "JobGoals",
                column: "createdByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobGoals_jobId_status",
                table: "JobGoals",
                columns: new[] { "jobId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobGoals_targetDate",
                table: "JobGoals",
                column: "targetDate");

            migrationBuilder.CreateIndex(
                name: "IX_JobGoals_verifiedBy",
                table: "JobGoals",
                column: "verifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JobGoalVotes_goalId_userId",
                table: "JobGoalVotes",
                columns: new[] { "goalId", "userId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobGoalVotes_userId",
                table: "JobGoalVotes",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_JobInventory_jobId",
                table: "JobInventory",
                column: "jobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobInventory_warehouseId",
                table: "JobInventory",
                column: "warehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_JobMedia_albumId",
                table: "JobMedia",
                column: "albumId");

            migrationBuilder.CreateIndex(
                name: "IX_JobMedia_jobId",
                table: "JobMedia",
                column: "jobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobMedia_jobInventoryId",
                table: "JobMedia",
                column: "jobInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_JobMilestones_completedBy",
                table: "JobMilestones",
                column: "completedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JobMilestones_jobId",
                table: "JobMilestones",
                column: "jobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobNotes_createdBy",
                table: "JobNotes",
                column: "createdBy");

            migrationBuilder.CreateIndex(
                name: "IX_JobNotes_jobId",
                table: "JobNotes",
                column: "jobId");

            migrationBuilder.CreateIndex(
                name: "IX_Location_clientId",
                table: "Location",
                column: "clientId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_companyId",
                table: "NotificationPreferences",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_userId_companyId",
                table: "NotificationPreferences",
                columns: new[] { "userId", "companyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_companyId",
                table: "Payments",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_jobId",
                table: "Payments",
                column: "jobId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_squarePaymentId",
                table: "Payments",
                column: "squarePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_squarePaymentLinkId",
                table: "Payments",
                column: "squarePaymentLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_status",
                table: "Payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_userId",
                table: "Payments",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickStartTemplates_category",
                table: "QuickStartTemplates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_TimeCards_approvedByUserId",
                table: "TimeCards",
                column: "approvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeCards_companyId_status",
                table: "TimeCards",
                columns: new[] { "companyId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeCards_companyId_userId_clockInTime",
                table: "TimeCards",
                columns: new[] { "companyId", "userId", "clockInTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeCards_jobId",
                table: "TimeCards",
                column: "jobId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeCards_userId",
                table: "TimeCards",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompany_companyId",
                table: "UserCompany",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_companyId",
                table: "UserPermissions",
                column: "companyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_grantedByUserId",
                table: "UserPermissions",
                column: "grantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_userId_companyId",
                table: "UserPermissions",
                columns: new[] { "userId", "companyId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_userId_companyId_permissionKey",
                table: "UserPermissions",
                columns: new[] { "userId", "companyId", "permissionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientNotes");

            migrationBuilder.DropTable(
                name: "CompanyConfig");

            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.DropTable(
                name: "EstimateMedia");

            migrationBuilder.DropTable(
                name: "InventoryBatch");

            migrationBuilder.DropTable(
                name: "InventoryCategory");

            migrationBuilder.DropTable(
                name: "InventoryUsageHistory");

            migrationBuilder.DropTable(
                name: "JobAssignments");

            migrationBuilder.DropTable(
                name: "JobGoalMedia");

            migrationBuilder.DropTable(
                name: "JobGoalVotes");

            migrationBuilder.DropTable(
                name: "JobMedia");

            migrationBuilder.DropTable(
                name: "JobMilestones");

            migrationBuilder.DropTable(
                name: "JobNotes");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "QuickStartTemplates");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "TimeCards");

            migrationBuilder.DropTable(
                name: "UserCompany");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "EstimateAlbums");

            migrationBuilder.DropTable(
                name: "EstimateItem");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.DropTable(
                name: "JobGoals");

            migrationBuilder.DropTable(
                name: "JobAlbums");

            migrationBuilder.DropTable(
                name: "JobInventory");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropTable(
                name: "EstimateTemplate");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Job");

            migrationBuilder.DropTable(
                name: "Warehouse");

            migrationBuilder.DropTable(
                name: "Estimates");

            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropTable(
                name: "Client");
        }
    }
}
