using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiReviewAndReflection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiReviewSummary",
                table: "CgiSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AiReviewedAtUtc",
                table: "CgiSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiRiskFlags",
                table: "CgiSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AiSuggestedAssessment",
                table: "CgiSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AiTeacherInsight",
                table: "CgiSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AnswerAnalysis",
                table: "CgiQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AnswerConfidence",
                table: "CgiQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "AnsweredAtUtc",
                table: "CgiQuestions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnswerSuspect",
                table: "CgiQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StudentAnswer",
                table: "CgiQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiReviewSummary",
                table: "CgiSessions");

            migrationBuilder.DropColumn(
                name: "AiReviewedAtUtc",
                table: "CgiSessions");

            migrationBuilder.DropColumn(
                name: "AiRiskFlags",
                table: "CgiSessions");

            migrationBuilder.DropColumn(
                name: "AiSuggestedAssessment",
                table: "CgiSessions");

            migrationBuilder.DropColumn(
                name: "AiTeacherInsight",
                table: "CgiSessions");

            migrationBuilder.DropColumn(
                name: "AnswerAnalysis",
                table: "CgiQuestions");

            migrationBuilder.DropColumn(
                name: "AnswerConfidence",
                table: "CgiQuestions");

            migrationBuilder.DropColumn(
                name: "AnsweredAtUtc",
                table: "CgiQuestions");

            migrationBuilder.DropColumn(
                name: "IsAnswerSuspect",
                table: "CgiQuestions");

            migrationBuilder.DropColumn(
                name: "StudentAnswer",
                table: "CgiQuestions");
        }
    }
}
