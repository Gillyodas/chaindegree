using System;
using System.Linq;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.Students;
using ChainDegree.Core.Domain.Universities;
using ChainDegree.Core.Domain.Universities.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence
{
    public static class ChainDegreeDbSeeder
    {
        public static async Task SeedAsync(ChainDegreeDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            var instId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var registrarUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            // 1. Seed Institution
            var hasInst = await context.EducationInstitutions.AnyAsync(x => x.Id == instId);
            if (!hasInst)
            {
                var inst = EducationInstitution.Create("UIT", "Đại học Công nghệ Thông tin - ĐHQG TP.HCM", "uit@edu.vn");
                SetId(inst, instId);
                context.EducationInstitutions.Add(inst);
                await context.SaveChangesAsync();
            }

            // 2. Seed Registrar
            var hasReg = await context.Registrars.AnyAsync(x => x.UserId == registrarUserId);
            if (!hasReg)
            {
                var registrar = Registrar.Create(instId, registrarUserId, "UIT-REG-001", "Học vụ UIT");
                SetId(registrar, Guid.NewGuid());
                context.Registrars.Add(registrar);
                await context.SaveChangesAsync();
            }

            // 3. Seed Students
            var studentAId = Guid.Parse("a2222222-2222-2222-2222-222222222222");
            var studentBId = Guid.Parse("b3333333-3333-3333-3333-333333333333");

            var hasStudentA = await context.Students.AnyAsync(x => x.Id == studentAId);
            if (!hasStudentA)
            {
                var studentA = Student.Create("079012345678", "Nguyễn Văn A", "nguyenvana@gmail.com", Guid.Parse("22222222-2222-2222-2222-222222222222")).Value;
                SetId(studentA, studentAId);
                context.Students.Add(studentA);
            }

            var hasStudentB = await context.Students.AnyAsync(x => x.Id == studentBId);
            if (!hasStudentB)
            {
                var studentB = Student.Create("079087654321", "Trần Thị B", "tranthib@gmail.com", Guid.Parse("33333333-3333-3333-3333-333333333333")).Value;
                SetId(studentB, studentBId);
                context.Students.Add(studentB);
            }

            await context.SaveChangesAsync();
        }

        private static void SetId(Entity entity, Guid id)
        {
            var prop = typeof(Entity).GetProperty("Id");
            prop?.SetValue(entity, id);
        }
    }
}
