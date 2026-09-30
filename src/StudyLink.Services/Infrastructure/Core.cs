using System;
using System.Collections.Generic;
using System.Linq;
using StudyLink.Data.Seed;

namespace StudyLink.Services.Infrastructure
{
    /// <summary>Server time. Everything is SAST local time: the hub, its campuses and its server share one zone.</summary>
    public interface IClock
    {
        DateTime Now { get; }
        DateTime Today { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
        public DateTime Today => DateTime.Today;
    }

    /// <summary>
    /// Who is performing an action. Built by the web layer from the authenticated ticket and passed
    /// into every service call, so services never read HttpContext and can be tested directly.
    /// Services re-check the roles they care about against the database where it matters (BR-24/25).
    /// </summary>
    public sealed class Actor
    {
        public Actor(int userId, string userName, IEnumerable<string> roles, string ipAddress = null)
        {
            UserId = userId;
            UserName = userName;
            Roles = new HashSet<string>(roles ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            IpAddress = ipAddress;
        }

        public int UserId { get; }
        public string UserName { get; }
        public ISet<string> Roles { get; }
        public string IpAddress { get; }

        public bool IsInRole(string role) => Roles.Contains(role);
        public bool IsInAnyRole(params string[] roles) => roles.Any(Roles.Contains);

        /// <summary>The role recorded in the audit trail: the most privileged one held.</summary>
        public string PrimaryRole =>
            new[] { RoleNames.SystemAdmin, RoleNames.Manager, RoleNames.Staff, RoleNames.Supplier, RoleNames.Tutor, RoleNames.Student }
                .FirstOrDefault(Roles.Contains) ?? "System";

        /// <summary>For actions the system takes on its own (background sweep, seeding).</summary>
        public static Actor System { get; } = new Actor(0, "system", new[] { "System" });

        public bool IsSystem => UserId == 0;
    }

    /// <summary>Ported from TutorConnect ServiceResult: success, or one user-facing message.</summary>
    public class ServiceResult
    {
        protected ServiceResult(bool succeeded, string error)
        {
            Succeeded = succeeded;
            Error = error;
        }

        public bool Succeeded { get; }
        public string Error { get; }

        public static ServiceResult Ok() => new ServiceResult(true, null);
        public static ServiceResult Fail(string error) => new ServiceResult(false, error);
    }

    public sealed class ServiceResult<T> : ServiceResult
    {
        private ServiceResult(bool succeeded, string error, T value) : base(succeeded, error)
        {
            Value = value;
        }

        public T Value { get; }

        public static ServiceResult<T> Ok(T value) => new ServiceResult<T>(true, null, value);
        public static new ServiceResult<T> Fail(string error) => new ServiceResult<T>(false, error, default(T));
    }

    /// <summary>Thrown when an actor attempts something their role does not permit. The web layer maps it to 403.</summary>
    public sealed class AccessDeniedException : Exception
    {
        public AccessDeniedException(string message) : base(message) { }
    }

    /// <summary>Recognises the SQL Server errors services translate into friendly messages.</summary>
    public static class DbErrors
    {
        /// <summary>2601/2627: a unique index or constraint rejected the write (e.g. a lost race).</summary>
        public static bool IsUniqueViolation(Exception ex) => HasSqlError(ex, 2601, 2627);

        /// <summary>1205: chosen as deadlock victim - report as "someone else got there first".</summary>
        public static bool IsDeadlock(Exception ex) => HasSqlError(ex, 1205);

        /// <summary>547: a CHECK or foreign-key constraint rejected the write.</summary>
        public static bool IsConstraintViolation(Exception ex) => HasSqlError(ex, 547);

        public static bool IsConcurrencyConflict(Exception ex) =>
            ex is System.Data.Entity.Infrastructure.DbUpdateConcurrencyException || IsDeadlock(ex) || IsUniqueViolation(ex);

        private static bool HasSqlError(Exception ex, params int[] numbers)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is System.Data.SqlClient.SqlException sql)
                    foreach (System.Data.SqlClient.SqlError err in sql.Errors)
                        if (numbers.Contains(err.Number))
                            return true;
            }
            return false;
        }
    }

    /// <summary>A page of results plus the total, for pagers.</summary>
    public sealed class PagedResult<T>
    {
        public PagedResult(IList<T> items, int total, int page, int pageSize)
        {
            Items = items;
            Total = total;
            Page = page;
            PageSize = pageSize;
        }

        public IList<T> Items { get; }
        public int Total { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int PageCount => PageSize <= 0 ? 1 : Math.Max(1, (Total + PageSize - 1) / PageSize);
    }
}
