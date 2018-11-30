﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UniFlowGW.Models;

namespace UniFlowGW.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024");

            modelBuilder.Entity("UniFlowGW.Models.Admin", b =>
                {
                    b.Property<int>("AdminId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Login");

                    b.Property<string>("PasswordHash");

                    b.HasKey("AdminId");

                    b.ToTable("Admins");
                });

            modelBuilder.Entity("UniFlowGW.Models.PrintTask", b =>
                {
                    b.Property<int>("PrintTaskId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Detail");

                    b.Property<string>("Document");

                    b.Property<string>("Message");

                    b.Property<bool>("QueuedTask");

                    b.Property<int>("Status");

                    b.Property<DateTime>("Time");

                    b.Property<string>("UserID");

                    b.HasKey("PrintTaskId");

                    b.HasIndex("Time");

                    b.HasIndex("UserID");

                    b.ToTable("PrintTasks");
                });

            modelBuilder.Entity("UniFlowGW.Models.WeChatUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("bindDate");

                    b.Property<string>("openId");

                    b.Property<string>("userId");

                    b.HasKey("Id");

                    b.ToTable("WeChatUsers");
                });
#pragma warning restore 612, 618
        }
    }
}