using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StudyLink.Tests.Integration
{
    [TestClass]
    [TestCategory("Integration")]
    public class AvatarTests
    {
        private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52 };

        [TestMethod]
        public void UploadIsCheckedByContentAndSize()
        {
            using (var w = new World())
            {
                var student = w.NewStudent();
                Assert.IsFalse(w.S.Profiles.SetPhoto(student, Encoding.UTF8.GetBytes("<script>alert(1)</script> renamed to photo.png")).Succeeded, "not an image");
                Assert.IsFalse(w.S.Profiles.SetPhoto(student, new byte[3 * 1024 * 1024]).Succeeded, "over 2 MB");
                Assert.IsTrue(w.S.Profiles.SetPhoto(student, Png).Succeeded);

                var own = w.S.Profiles.Photo(student, student.UserId);
                Assert.IsNotNull(own, "everyone sees their own photo");
                Assert.AreEqual("image/png", own.ContentType, "type comes from the bytes");
                Assert.IsFalse(own.IsPublic, "a student's photo is never publicly cacheable");

                Assert.IsTrue(w.S.Profiles.RemovePhoto(student).Succeeded);
                Assert.IsNull(w.S.Profiles.Photo(student, student.UserId), "back to the default avatar");
            }
        }

        [TestMethod]
        public void StudentPhoto_HiddenFromPublicAndOtherStudents_VisibleToStaffAndTheirTutor()
        {
            using (var w = new World())
            {
                var student = w.NewStudent();
                Assert.IsTrue(w.S.Profiles.SetPhoto(student, Png).Succeeded);

                Assert.IsNull(w.S.Profiles.Photo(null, student.UserId), "anonymous visitor");
                Assert.IsNull(w.S.Profiles.Photo(w.NewStudent(), student.UserId), "another student");
                Assert.IsNotNull(w.S.Profiles.Photo(w.NewStaff(), student.UserId), "hub staff");
                Assert.IsNotNull(w.S.Profiles.Photo(w.NewManager(), student.UserId), "manager");

                var tutor = w.NewTutor();
                Assert.IsNull(w.S.Profiles.Photo(tutor, student.UserId), "a tutor who never taught this student");
                var slot = w.Slot(tutor, w.Clock.Today.AddDays(2), 10, 11);
                Assert.IsTrue(w.S.Bookings.Reserve(student, slot, null).Succeeded);
                Assert.IsNotNull(w.S.Profiles.Photo(tutor, student.UserId), "the student's own tutor");
            }
        }

        [TestMethod]
        public void TutorPhoto_PublicOnlyOnceVerified()
        {
            using (var w = new World())
            {
                var pending = w.NewTutor(verify: false);
                Assert.IsTrue(w.S.Profiles.SetPhoto(pending, Png).Succeeded);
                Assert.IsNull(w.S.Profiles.Photo(null, pending.UserId), "not in the public directory yet");
                Assert.IsNotNull(w.S.Profiles.Photo(w.NewStaff(), pending.UserId), "staff verifying the tutor can see it");

                var verified = w.NewTutor();
                Assert.IsTrue(w.S.Profiles.SetPhoto(verified, Png).Succeeded);
                var shown = w.S.Profiles.Photo(null, verified.UserId);
                Assert.IsNotNull(shown, "tutor directory is public");
                Assert.IsTrue(shown.IsPublic, "public tutor photos may be cached for a day");
            }
        }

        [TestMethod]
        public void StaffCannotSeeManagerOrAdminPhotos()
        {
            using (var w = new World())
            {
                var manager = w.NewManager();
                var admin = w.Admin();
                Assert.IsTrue(w.S.Profiles.SetPhoto(manager, Png).Succeeded);
                Assert.IsTrue(w.S.Profiles.SetPhoto(admin, Png).Succeeded);

                var staff = w.NewStaff();
                Assert.IsNull(w.S.Profiles.Photo(staff, manager.UserId));
                Assert.IsNull(w.S.Profiles.Photo(staff, admin.UserId));
                Assert.IsNotNull(w.S.Profiles.Photo(w.NewManager(), manager.UserId), "managers see managers");
                Assert.IsNull(w.S.Profiles.Photo(w.NewManager(), admin.UserId), "but not admins");
                Assert.IsNotNull(w.S.Profiles.Photo(admin, manager.UserId), "admins see everyone");
            }
        }
    }
}
