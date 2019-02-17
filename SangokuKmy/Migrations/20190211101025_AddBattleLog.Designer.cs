﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SangokuKmy.Models.Data;

namespace SangokuKmy.Migrations
{
    [DbContext(typeof(MainContext))]
    [Migration("20190211101025_AddBattleLog")]
    partial class AddBattleLog
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.AuthenticationData", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("AccessToken")
                        .HasColumnName("access_token")
                        .HasColumnType("varchar(256)");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<DateTime>("ExpirationTime")
                        .HasColumnName("expiration_time");

                    b.Property<uint>("Scope")
                        .HasColumnName("scope");

                    b.HasKey("Id");

                    b.ToTable("authentication_data");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.BattleLog", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("AttackerCacheId")
                        .HasColumnName("attacker_cache_id");

                    b.Property<uint>("AttackerCharacterId")
                        .HasColumnName("attacker_character_id");

                    b.Property<uint>("DefenderCacheId")
                        .HasColumnName("defender_cache_id");

                    b.Property<uint>("DefenderCharacterId")
                        .HasColumnName("defender_character_id");

                    b.Property<short>("IntDefenderType")
                        .HasColumnName("defender_type");

                    b.Property<uint>("MapLogId")
                        .HasColumnName("maplog_id");

                    b.Property<uint>("TownId")
                        .HasColumnName("town_id");

                    b.HasKey("Id");

                    b.ToTable("battle_logs");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.BattleLogLine", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<short>("AttackerDamage")
                        .HasColumnName("attacker_damage");

                    b.Property<short>("AttackerNumber")
                        .HasColumnName("attacker_number");

                    b.Property<uint>("BattleLogId")
                        .HasColumnName("battle_log_id");

                    b.Property<short>("DefenderDamage")
                        .HasColumnName("defender_damage");

                    b.Property<short>("DefenderNumber")
                        .HasColumnName("defender_number");

                    b.Property<short>("Turn")
                        .HasColumnName("turn");

                    b.HasKey("Id");

                    b.ToTable("battle_log_lines");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.Character", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("AliasId")
                        .HasColumnName("alias_id")
                        .HasColumnType("varchar(32)");

                    b.Property<int>("Class")
                        .HasColumnName("class");

                    b.Property<int>("Contribution")
                        .HasColumnName("contribution");

                    b.Property<uint>("CountryId")
                        .HasColumnName("country_id");

                    b.Property<short>("DeleteTurn")
                        .HasColumnName("delete_turn");

                    b.Property<int>("IntLastUpdatedGameDate")
                        .HasColumnName("last_updated_game_date");

                    b.Property<short>("Intellect")
                        .HasColumnName("intellect");

                    b.Property<short>("IntellectEx")
                        .HasColumnName("intellect_ex");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnName("last_updated");

                    b.Property<short>("Leadership")
                        .HasColumnName("leadership");

                    b.Property<short>("LeadershipEx")
                        .HasColumnName("leadership_ex");

                    b.Property<string>("Message")
                        .HasColumnName("message");

                    b.Property<int>("Money")
                        .HasColumnName("money");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("varchar(64)");

                    b.Property<string>("PasswordHash")
                        .HasColumnName("password_hash")
                        .HasColumnType("varchar(256)");

                    b.Property<short>("Popularity")
                        .HasColumnName("popularity");

                    b.Property<short>("PopularityEx")
                        .HasColumnName("popularity_ex");

                    b.Property<short>("Proficiency")
                        .HasColumnName("proficiency");

                    b.Property<int>("Rice")
                        .HasColumnName("rice");

                    b.Property<int>("SoldierNumber")
                        .HasColumnName("soldier_number");

                    b.Property<short>("SoldierType")
                        .HasColumnName("soldier_type");

                    b.Property<short>("Strong")
                        .HasColumnName("strong");

                    b.Property<short>("StrongEx")
                        .HasColumnName("strong_ex");

                    b.Property<uint>("TownId")
                        .HasColumnName("town_id");

                    b.HasKey("Id");

                    b.ToTable("characters");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.CharacterCommand", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<int>("IntGameDateTime")
                        .HasColumnName("game_date");

                    b.Property<short>("Type")
                        .HasColumnName("type");

                    b.HasKey("Id");

                    b.ToTable("character_commands");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.CharacterCommandParameter", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterCommandId")
                        .HasColumnName("character_command_id");

                    b.Property<int?>("NumberValue")
                        .HasColumnName("number_value");

                    b.Property<string>("StringValue")
                        .HasColumnName("string_value");

                    b.Property<int>("Type")
                        .HasColumnName("type");

                    b.HasKey("Id");

                    b.ToTable("character_command_parameters");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.CharacterIcon", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<string>("FileName")
                        .HasColumnName("file_name");

                    b.Property<bool>("IsAvailable")
                        .HasColumnName("is_available");

                    b.Property<bool>("IsMain")
                        .HasColumnName("is_main");

                    b.Property<byte>("Type")
                        .HasColumnName("type");

                    b.Property<string>("Uri")
                        .HasColumnName("uri");

                    b.HasKey("Id");

                    b.ToTable("character_icons");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.CharacterLog", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<DateTime>("DateTime")
                        .HasColumnName("date");

                    b.Property<int>("IntGameDateTime")
                        .HasColumnName("game_date");

                    b.Property<string>("Message")
                        .HasColumnName("message");

                    b.HasKey("Id");

                    b.ToTable("character_logs");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.CharacterUpdateLog", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<DateTime>("DateTime")
                        .HasColumnName("date");

                    b.Property<int>("IntGameDateTime")
                        .HasColumnName("game_date");

                    b.Property<bool>("IsFirstAtMonth")
                        .HasColumnName("is_first_at_month");

                    b.HasKey("Id");

                    b.ToTable("CharacterUpdateLogs");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.ChatMessage", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterCountryId")
                        .HasColumnName("character_country_id");

                    b.Property<uint>("CharacterIconId")
                        .HasColumnName("character_icon_id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<string>("Message")
                        .HasColumnName("message");

                    b.Property<DateTime>("Posted")
                        .HasColumnName("posted");

                    b.Property<short>("Type")
                        .HasColumnName("type");

                    b.Property<uint>("TypeData")
                        .HasColumnName("type_data");

                    b.Property<uint>("TypeData2")
                        .HasColumnName("type_data_2");

                    b.HasKey("Id");

                    b.ToTable("char_messages");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.Country", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CapitalTownId")
                        .HasColumnName("capital_town_id");

                    b.Property<short>("CountryColorId")
                        .HasColumnName("country_color_id");

                    b.Property<bool>("HasOverthrown")
                        .HasColumnName("has_overthrown");

                    b.Property<int>("IntEstablished")
                        .HasColumnName("established");

                    b.Property<int>("IntOverthrownGameDate")
                        .HasColumnName("overthrown_game_date");

                    b.Property<int>("LastMoneyIncomes")
                        .HasColumnName("last_money_incomes");

                    b.Property<int>("LastRiceIncomes")
                        .HasColumnName("last_rice_incomes");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("varchar(64)");

                    b.HasKey("Id");

                    b.ToTable("countries");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.CountryAlliance", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<int>("BreakingDelay")
                        .HasColumnName("breaking_delay");

                    b.Property<uint>("InsistedCountryId")
                        .HasColumnName("insisted_country_id");

                    b.Property<bool>("IsPublic")
                        .HasColumnName("is_public");

                    b.Property<int>("NewBreakingDelay")
                        .HasColumnName("new_breaking_delay");

                    b.Property<uint>("RequestedCountryId")
                        .HasColumnName("requested_country_id");

                    b.Property<short>("Status")
                        .HasColumnName("status");

                    b.HasKey("Id");

                    b.ToTable("country_alliances");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.CountryMessage", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CountryId")
                        .HasColumnName("country_id");

                    b.Property<string>("Message")
                        .HasColumnName("message");

                    b.Property<byte>("Type")
                        .HasColumnName("type");

                    b.HasKey("Id");

                    b.ToTable("country_messages");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.CountryPost", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<uint>("CountryId")
                        .HasColumnName("country_id");

                    b.Property<short>("Type")
                        .HasColumnName("type");

                    b.HasKey("Id");

                    b.ToTable("country_posts");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.CountryWar", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("InsistedCountryId")
                        .HasColumnName("insisted_country_id");

                    b.Property<int>("IntStartGameDate")
                        .HasColumnName("start_game_date");

                    b.Property<uint>("RequestedCountryId")
                        .HasColumnName("requested_country_id");

                    b.Property<uint>("RequestedStopCountryId")
                        .HasColumnName("requested_stop_country_id");

                    b.Property<short>("Status")
                        .HasColumnName("status");

                    b.HasKey("Id");

                    b.ToTable("country_wars");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.DefaultIconData", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("FileName")
                        .HasColumnName("file_name")
                        .HasColumnType("varchar(256)");

                    b.HasKey("Id");

                    b.ToTable("m_default_icons");

                    b.HasData(
                        new { Id = 1u, FileName = "0.gif" },
                        new { Id = 2u, FileName = "1.gif" },
                        new { Id = 3u, FileName = "2.gif" },
                        new { Id = 4u, FileName = "3.gif" },
                        new { Id = 5u, FileName = "4.gif" },
                        new { Id = 6u, FileName = "5.gif" },
                        new { Id = 7u, FileName = "6.gif" },
                        new { Id = 8u, FileName = "7.gif" },
                        new { Id = 9u, FileName = "8.gif" },
                        new { Id = 10u, FileName = "9.gif" },
                        new { Id = 11u, FileName = "10.gif" },
                        new { Id = 12u, FileName = "11.gif" },
                        new { Id = 13u, FileName = "12.gif" },
                        new { Id = 14u, FileName = "13.gif" },
                        new { Id = 15u, FileName = "14.gif" },
                        new { Id = 16u, FileName = "15.gif" },
                        new { Id = 17u, FileName = "16.gif" },
                        new { Id = 18u, FileName = "17.gif" },
                        new { Id = 19u, FileName = "18.gif" },
                        new { Id = 20u, FileName = "19.gif" },
                        new { Id = 21u, FileName = "20.gif" },
                        new { Id = 22u, FileName = "21.gif" },
                        new { Id = 23u, FileName = "22.gif" },
                        new { Id = 24u, FileName = "23.gif" },
                        new { Id = 25u, FileName = "24.gif" },
                        new { Id = 26u, FileName = "25.gif" },
                        new { Id = 27u, FileName = "26.gif" },
                        new { Id = 28u, FileName = "27.gif" },
                        new { Id = 29u, FileName = "28.gif" },
                        new { Id = 30u, FileName = "29.gif" },
                        new { Id = 31u, FileName = "30.gif" },
                        new { Id = 32u, FileName = "31.gif" },
                        new { Id = 33u, FileName = "32.gif" },
                        new { Id = 34u, FileName = "33.gif" },
                        new { Id = 35u, FileName = "34.gif" },
                        new { Id = 36u, FileName = "35.gif" },
                        new { Id = 37u, FileName = "36.gif" },
                        new { Id = 38u, FileName = "37.gif" },
                        new { Id = 39u, FileName = "38.gif" },
                        new { Id = 40u, FileName = "39.gif" },
                        new { Id = 41u, FileName = "40.gif" },
                        new { Id = 42u, FileName = "41.gif" },
                        new { Id = 43u, FileName = "42.gif" },
                        new { Id = 44u, FileName = "43.gif" },
                        new { Id = 45u, FileName = "44.gif" },
                        new { Id = 46u, FileName = "45.gif" },
                        new { Id = 47u, FileName = "46.gif" },
                        new { Id = 48u, FileName = "47.gif" },
                        new { Id = 49u, FileName = "48.gif" },
                        new { Id = 50u, FileName = "49.gif" },
                        new { Id = 51u, FileName = "50.gif" },
                        new { Id = 52u, FileName = "51.gif" },
                        new { Id = 53u, FileName = "52.gif" },
                        new { Id = 54u, FileName = "53.gif" },
                        new { Id = 55u, FileName = "54.gif" },
                        new { Id = 56u, FileName = "55.gif" },
                        new { Id = 57u, FileName = "56.gif" },
                        new { Id = 58u, FileName = "57.gif" },
                        new { Id = 59u, FileName = "58.gif" },
                        new { Id = 60u, FileName = "59.gif" },
                        new { Id = 61u, FileName = "60.gif" },
                        new { Id = 62u, FileName = "61.gif" },
                        new { Id = 63u, FileName = "62.gif" },
                        new { Id = 64u, FileName = "63.gif" },
                        new { Id = 65u, FileName = "64.gif" },
                        new { Id = 66u, FileName = "65.gif" },
                        new { Id = 67u, FileName = "66.gif" },
                        new { Id = 68u, FileName = "67.gif" },
                        new { Id = 69u, FileName = "68.gif" },
                        new { Id = 70u, FileName = "69.gif" },
                        new { Id = 71u, FileName = "70.gif" },
                        new { Id = 72u, FileName = "71.gif" },
                        new { Id = 73u, FileName = "72.gif" },
                        new { Id = 74u, FileName = "73.gif" },
                        new { Id = 75u, FileName = "74.gif" },
                        new { Id = 76u, FileName = "75.gif" },
                        new { Id = 77u, FileName = "76.gif" },
                        new { Id = 78u, FileName = "77.gif" },
                        new { Id = 79u, FileName = "78.gif" },
                        new { Id = 80u, FileName = "79.gif" },
                        new { Id = 81u, FileName = "80.gif" },
                        new { Id = 82u, FileName = "81.gif" },
                        new { Id = 83u, FileName = "82.gif" },
                        new { Id = 84u, FileName = "83.gif" },
                        new { Id = 85u, FileName = "84.gif" },
                        new { Id = 86u, FileName = "85.gif" },
                        new { Id = 87u, FileName = "86.gif" },
                        new { Id = 88u, FileName = "87.gif" },
                        new { Id = 89u, FileName = "88.gif" },
                        new { Id = 90u, FileName = "89.gif" },
                        new { Id = 91u, FileName = "90.gif" },
                        new { Id = 92u, FileName = "91.gif" },
                        new { Id = 93u, FileName = "92.gif" },
                        new { Id = 94u, FileName = "93.gif" },
                        new { Id = 95u, FileName = "94.gif" },
                        new { Id = 96u, FileName = "95.gif" },
                        new { Id = 97u, FileName = "96.gif" },
                        new { Id = 98u, FileName = "97.gif" },
                        new { Id = 99u, FileName = "98.gif" }
                    );
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.LogCharacterCache", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<uint>("CountryId")
                        .HasColumnName("country_id");

                    b.Property<uint>("IconId")
                        .HasColumnName("icon_id");

                    b.Property<short>("Intellect")
                        .HasColumnName("intellect");

                    b.Property<short>("Leadership")
                        .HasColumnName("leadership");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("varchar(64)");

                    b.Property<short>("Popularity")
                        .HasColumnName("popularity");

                    b.Property<short>("Proficiency")
                        .HasColumnName("proficiency");

                    b.Property<int>("SoldierNumber")
                        .HasColumnName("soldier_number");

                    b.Property<short>("SoldierType")
                        .HasColumnName("soldier_type");

                    b.Property<short>("Strong")
                        .HasColumnName("strong");

                    b.HasKey("Id");

                    b.ToTable("character_caches");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.MapLog", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("BattleLogId")
                        .HasColumnName("battle_log_id");

                    b.Property<DateTime>("Date")
                        .HasColumnName("date");

                    b.Property<short>("EventType")
                        .HasColumnName("event_type");

                    b.Property<int>("IntGameDateTime")
                        .HasColumnName("game_date");

                    b.Property<bool>("IsImportant")
                        .HasColumnName("is_important");

                    b.Property<string>("Message")
                        .HasColumnName("message");

                    b.HasKey("Id");

                    b.ToTable("map_logs");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.ScoutedCharacter", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<uint>("ScoutId")
                        .HasColumnName("scout_id");

                    b.Property<int>("SoldierNumber")
                        .HasColumnName("soldier_number");

                    b.Property<short>("SoldierType")
                        .HasColumnName("soldier_type");

                    b.HasKey("Id");

                    b.ToTable("scouted_characters");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.ScoutedDefender", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<uint>("ScoutId")
                        .HasColumnName("scout_id");

                    b.Property<int>("SoldierNumber")
                        .HasColumnName("soldier_number");

                    b.Property<short>("SoldierType")
                        .HasColumnName("soldier_type");

                    b.HasKey("Id");

                    b.ToTable("scouted_defenders");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.ScoutedTown", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<int>("Agriculture")
                        .HasColumnName("agriculture");

                    b.Property<int>("AgricultureMax")
                        .HasColumnName("agriculture_max");

                    b.Property<int>("Commercial")
                        .HasColumnName("commercial");

                    b.Property<int>("CommercialMax")
                        .HasColumnName("commercial_max");

                    b.Property<uint>("CountryId")
                        .HasColumnName("country_id");

                    b.Property<int>("IntRicePrice")
                        .HasColumnName("rice_price");

                    b.Property<int>("IntScoutedDateTime")
                        .HasColumnName("scouted_game_date_time");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("varchar(64)");

                    b.Property<int>("People")
                        .HasColumnName("people");

                    b.Property<short>("ScoutMethod")
                        .HasColumnName("scout_method");

                    b.Property<uint>("ScoutedCharacterId")
                        .HasColumnName("scouted_character_id");

                    b.Property<uint>("ScoutedCountryId")
                        .HasColumnName("scouted_country_id");

                    b.Property<uint>("ScoutedTownId")
                        .HasColumnName("scouted_town_id");

                    b.Property<short>("Security")
                        .HasColumnName("security");

                    b.Property<int>("Technology")
                        .HasColumnName("technology");

                    b.Property<int>("TechnologyMax")
                        .HasColumnName("technology_max");

                    b.Property<byte>("Type")
                        .HasColumnName("type");

                    b.Property<int>("Wall")
                        .HasColumnName("wall");

                    b.Property<int>("WallGuard")
                        .HasColumnName("wallguard");

                    b.Property<int>("WallGuardMax")
                        .HasColumnName("wallguard_max");

                    b.Property<int>("WallMax")
                        .HasColumnName("wall_max");

                    b.Property<short>("X")
                        .HasColumnName("x");

                    b.Property<short>("Y")
                        .HasColumnName("y");

                    b.HasKey("Id");

                    b.ToTable("scouted_town");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.SystemData", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<short>("BetaVersion")
                        .HasColumnName("beta_version");

                    b.Property<DateTime>("CurrentMonthStartDateTime")
                        .HasColumnName("current_month_start_date_time");

                    b.Property<int>("IntGameDateTime")
                        .HasColumnName("game_date_time");

                    b.Property<bool>("IsDebug")
                        .HasColumnName("is_debug");

                    b.Property<short>("Period")
                        .HasColumnName("period");

                    b.HasKey("Id");

                    b.ToTable("system_data");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.SystemDebugData", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<DateTime>("UpdatableLastDateTime")
                        .HasColumnName("updatable_last_date");

                    b.HasKey("Id");

                    b.ToTable("system_debug");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.Town", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<int>("Agriculture")
                        .HasColumnName("agriculture");

                    b.Property<int>("AgricultureMax")
                        .HasColumnName("agriculture_max");

                    b.Property<int>("Commercial")
                        .HasColumnName("commercial");

                    b.Property<int>("CommercialMax")
                        .HasColumnName("commercial_max");

                    b.Property<uint>("CountryId")
                        .HasColumnName("country_id");

                    b.Property<int>("IntRicePrice")
                        .HasColumnName("rice_price");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("varchar(64)");

                    b.Property<int>("People")
                        .HasColumnName("people");

                    b.Property<short>("Security")
                        .HasColumnName("security");

                    b.Property<int>("Technology")
                        .HasColumnName("technology");

                    b.Property<int>("TechnologyMax")
                        .HasColumnName("technology_max");

                    b.Property<byte>("Type")
                        .HasColumnName("type");

                    b.Property<int>("Wall")
                        .HasColumnName("wall");

                    b.Property<int>("WallGuard")
                        .HasColumnName("wallguard");

                    b.Property<int>("WallGuardMax")
                        .HasColumnName("wallguard_max");

                    b.Property<int>("WallMax")
                        .HasColumnName("wall_max");

                    b.Property<short>("X")
                        .HasColumnName("x");

                    b.Property<short>("Y")
                        .HasColumnName("y");

                    b.HasKey("Id");

                    b.ToTable("town");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.TownDefender", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<uint>("TownId")
                        .HasColumnName("town_id");

                    b.HasKey("Id");

                    b.ToTable("town_defenders");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.Unit", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CountryId")
                        .HasColumnName("country_id");

                    b.Property<bool>("IsLimited")
                        .HasColumnName("is_limited");

                    b.Property<string>("Message")
                        .HasColumnName("message");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("varchar(64)");

                    b.HasKey("Id");

                    b.ToTable("units");
                });

            modelBuilder.Entity("SangokuKmy.Models.Data.Entities.UnitMember", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<uint>("CharacterId")
                        .HasColumnName("character_id");

                    b.Property<byte>("Post")
                        .HasColumnName("post");

                    b.Property<uint>("UnitId")
                        .HasColumnName("unit_id");

                    b.HasKey("Id");

                    b.ToTable("unit_members");
                });
#pragma warning restore 612, 618
        }
    }
}
