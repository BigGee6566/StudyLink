using System.Collections.Generic;
using System.Linq;
using StudyLink.Data.Seed;

namespace StudyLink.Services.Policies
{
    /// <summary>Who is looking at a profile photo. Anonymous visitors are Public.</summary>
    public enum AvatarViewer { Public, Student, Supplier, Tutor, Staff, Manager, Admin }

    /// <summary>Whose photo it is. A tutor's photo is public only once the hub has verified them.</summary>
    public enum AvatarOwner { Student, VerifiedTutor, UnverifiedTutor, Staff, Supplier, Manager, Admin }

    public enum AvatarAccess { None, View, OwnStudentsOnly }

    /// <summary>
    /// The profile-photo access matrix. Everyone may see and change their own photo; otherwise a viewer
    /// sees a photo only where the table says so, and a hidden photo is answered exactly like a missing
    /// one (404). A user with several roles gets the most generous of their roles as a viewer, and the
    /// most private of their roles as an owner.
    /// </summary>
    public static class AvatarPolicy
    {
        private static readonly Dictionary<(AvatarViewer, AvatarOwner), AvatarAccess> Matrix = Build();

        private static Dictionary<(AvatarViewer, AvatarOwner), AvatarAccess> Build()
        {
            var m = new Dictionary<(AvatarViewer, AvatarOwner), AvatarAccess>();
            void Allow(AvatarViewer viewer, AvatarAccess access, params AvatarOwner[] owners)
            {
                foreach (var owner in owners) m[(viewer, owner)] = access;
            }

            // The tutor directory is public: anyone may see a verified tutor's photo.
            foreach (var viewer in new[] { AvatarViewer.Public, AvatarViewer.Student, AvatarViewer.Supplier, AvatarViewer.Tutor })
                Allow(viewer, AvatarAccess.View, AvatarOwner.VerifiedTutor);
            Allow(AvatarViewer.Tutor, AvatarAccess.OwnStudentsOnly, AvatarOwner.Student);
            Allow(AvatarViewer.Staff, AvatarAccess.View,
                AvatarOwner.Student, AvatarOwner.VerifiedTutor, AvatarOwner.UnverifiedTutor, AvatarOwner.Staff);
            Allow(AvatarViewer.Manager, AvatarAccess.View,
                AvatarOwner.Student, AvatarOwner.VerifiedTutor, AvatarOwner.UnverifiedTutor, AvatarOwner.Staff, AvatarOwner.Supplier, AvatarOwner.Manager);
            Allow(AvatarViewer.Admin, AvatarAccess.View,
                AvatarOwner.Student, AvatarOwner.VerifiedTutor, AvatarOwner.UnverifiedTutor, AvatarOwner.Staff, AvatarOwner.Supplier,
                AvatarOwner.Manager, AvatarOwner.Admin);
            return m;
        }

        public static AvatarAccess Access(AvatarViewer viewer, AvatarOwner owner) =>
            Matrix.TryGetValue((viewer, owner), out var access) ? access : AvatarAccess.None;

        /// <param name="isOwn">The viewer is the photo's owner.</param>
        /// <param name="tutorsOwner">The viewer is a tutor who has (or had) a booking with this student.</param>
        public static bool CanView(bool isOwn, IEnumerable<AvatarViewer> viewer, AvatarOwner owner, bool tutorsOwner)
        {
            if (isOwn) return true;
            return viewer.Any(v =>
            {
                var access = Access(v, owner);
                return access == AvatarAccess.View || (access == AvatarAccess.OwnStudentsOnly && tutorsOwner);
            });
        }

        /// <summary>Only a verified tutor's photo may be cached by shared caches.</summary>
        public static bool IsPublic(AvatarOwner owner) => owner == AvatarOwner.VerifiedTutor;

        public static IList<AvatarViewer> ViewerKinds(IEnumerable<string> roles)
        {
            var kinds = (roles ?? Enumerable.Empty<string>()).Select(r =>
                r == RoleNames.SystemAdmin ? AvatarViewer.Admin
                : r == RoleNames.Manager ? AvatarViewer.Manager
                : r == RoleNames.Staff ? AvatarViewer.Staff
                : r == RoleNames.Tutor ? AvatarViewer.Tutor
                : r == RoleNames.Supplier ? AvatarViewer.Supplier
                : r == RoleNames.Student ? AvatarViewer.Student
                : AvatarViewer.Public).ToList();
            if (kinds.Count == 0) kinds.Add(AvatarViewer.Public);
            return kinds;
        }

        /// <summary>The most private kind among the owner's roles.</summary>
        public static AvatarOwner OwnerKind(IEnumerable<string> roles, bool tutorVerified)
        {
            var set = new HashSet<string>(roles ?? Enumerable.Empty<string>());
            if (set.Contains(RoleNames.SystemAdmin)) return AvatarOwner.Admin;
            if (set.Contains(RoleNames.Manager)) return AvatarOwner.Manager;
            if (set.Contains(RoleNames.Supplier)) return AvatarOwner.Supplier;
            if (set.Contains(RoleNames.Staff)) return AvatarOwner.Staff;
            if (set.Contains(RoleNames.Student)) return AvatarOwner.Student;
            if (set.Contains(RoleNames.Tutor)) return tutorVerified ? AvatarOwner.VerifiedTutor : AvatarOwner.UnverifiedTutor;
            return AvatarOwner.Admin; // no recognised role: visible to admins only
        }
    }
}
