using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController(IDashboardService dashboardService, AppDbContext db) : ControllerBase
    {
        [HttpGet("student/{childName}")]
        [ProducesResponseType(typeof(StudentDashboardDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStudent(string childName)
        {
            var data = await dashboardService.GetStudentDashboardAsync(childName);
            if (data is null)
                return NotFound(new { error = "لم يتم العثور على بيانات لهذا الطالب." });
            return Ok(data);
        }

        [HttpGet("parent/{childName}")]
        [ProducesResponseType(typeof(ParentDashboardDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetParent(string childName)
        {
            var data = await dashboardService.GetParentDashboardAsync(childName);
            if (data is null)
                return NotFound(new { error = "لم يتم العثور على بيانات لهذا الطفل." });
            return Ok(data);
        }

        [HttpGet("teacher")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(typeof(TeacherDashboardDto), 200)]
        public async Task<IActionResult> GetTeacher()
        {
            var teacherId = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            var data = await dashboardService.GetTeacherDashboardAsync(teacherId);
            return Ok(data);
        }

        [HttpGet("school")]
        [Authorize(Roles = "SchoolAdmin,Teacher,SystemAdmin")]
        [ProducesResponseType(typeof(SchoolDashboardDto), 200)]
        public async Task<IActionResult> GetSchool()
        {
            var userId = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            string schoolCode;

            if (role == "Teacher")
            {
                var teacher = await db.Teachers.FindAsync(userId);
                if (teacher is null || string.IsNullOrEmpty(teacher.SchoolCode))
                    return Forbid();
                schoolCode = teacher.SchoolCode;
            }
            else
            {
                // SchoolAdmin and SystemAdmin: schoolCode derived from userId
                schoolCode = userId.ToString("N")[..8].ToUpper();
            }

            var data = await dashboardService.GetSchoolDashboardAsync(schoolCode);
            return Ok(data);
        }

        [HttpGet("students")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public async Task<IActionResult> GetStudentNames()
        {
            var names = await dashboardService.GetKnownChildNamesAsync();
            return Ok(names);
        }

        [HttpGet("levels/progress/{childName}")]
        [ProducesResponseType(typeof(List<LevelProgressDto>), 200)]
        public async Task<IActionResult> GetLevelProgress(string childName)
        {
            var data = await dashboardService.GetLevelProgressAsync(childName);
            return Ok(data);
        }
    }
}
