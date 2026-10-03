using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StudyLink.Data.Seed;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Tests.Integration
{
    [TestClass]
    [TestCategory("Integration")]
    public class TemporaryPasswordTests
    {
        private const string Temporary = "Welcome2026x";

        [TestMethod]
        public void NewAccount_SignsInWithTheEmailedTemporaryPassword_ThenMustChooseItsOwn()
        {
            using (var w = new World())
            {
                var manager = w.NewManager();
                var x = World.Suffix();
                CreateAccountRequest Staff(string temporary) => new CreateAccountRequest
                {
                    Role = RoleNames.Staff, UserName = "temp_" + x, FirstName = "Olona", LastName = "Vane", Email = "temp_" + x + "@test.local",
                    CellNumber = "0841234567", CampusId = w.Campus(), TemporaryPassword = temporary
                };

                Assert.IsFalse(w.S.Users.CreateAccount(manager, Staff(null), "https://test/Account/Login.aspx").Succeeded, "a temporary password is required");
                Assert.IsFalse(w.S.Users.CreateAccount(manager, Staff("letters-only"), "https://test/Account/Login.aspx").Succeeded, "the normal password rule applies");

                var created = w.S.Users.CreateAccount(manager, Staff(Temporary), "https://test/Account/Login.aspx");
                Assert.IsTrue(created.Succeeded, created.Error);
                Assert.IsTrue(created.Value.PasswordEmailed);

                var mail = w.Email.Sent.Single(m => m.Item1 == "temp_" + x + "@test.local");
                StringAssert.Contains(mail.Item3, "temp_" + x, "the email gives the username");
                StringAssert.Contains(mail.Item3, Temporary, "and the temporary password");
                StringAssert.Contains(mail.Item3, "https://test/Account/Login.aspx", "and where to sign in");

                var first = w.S.Auth.SignIn("temp_" + x, Temporary, "t");
                Assert.IsTrue(first.Succeeded, first.Error);
                Assert.IsTrue(first.MustChangePassword, "a password chosen by someone else must be replaced");

                var user = w.ActorFor(created.Value.UserId);
                Assert.IsTrue(w.S.Auth.ChangePassword(user, Temporary, "MyOwnPass99", "MyOwnPass99").Succeeded);
                Assert.IsFalse(w.S.Auth.SignIn("temp_" + x, "MyOwnPass99", "t").MustChangePassword);
            }
        }

        [TestMethod]
        public void ManagersAndAdmins_CanReplaceAPassword_WithinTheirScope()
        {
            using (var w = new World())
            {
                var manager = w.NewManager();
                var student = w.NewStudent();

                var set = w.S.Users.SetTemporaryPassword(manager, student.UserId, Temporary, "https://test/Account/Login.aspx");
                Assert.IsTrue(set.Succeeded, set.Error);
                Assert.IsTrue(set.Value, "emailed");
                Assert.IsFalse(w.S.Auth.SignIn(student.UserName, World.Password, "t").Succeeded, "the old password no longer works");
                var signIn = w.S.Auth.SignIn(student.UserName, Temporary, "t");
                Assert.IsTrue(signIn.Succeeded && signIn.MustChangePassword);
                Assert.IsTrue(w.Email.Sent.Any(m => m.Item2 == "Your new StudyLink password" && m.Item3.Contains(Temporary)));

                Assert.IsFalse(w.S.Users.SetTemporaryPassword(manager, manager.UserId, Temporary, "u").Succeeded, "not for your own account");
                Assert.IsFalse(w.S.Users.SetTemporaryPassword(manager, student.UserId, "short1", "u").Succeeded, "the password rule applies");
                Assert.ThrowsException<AccessDeniedException>(() => w.S.Users.SetTemporaryPassword(manager, w.Admin().UserId, Temporary, "u"), "managers cannot touch admins");
                Assert.ThrowsException<AccessDeniedException>(() => w.S.Users.SetTemporaryPassword(w.NewStaff(), student.UserId, Temporary, "u"), "front-desk staff cannot");
                Assert.IsTrue(w.S.Users.SetTemporaryPassword(w.Admin(), manager.UserId, Temporary, "u").Succeeded, "admins can for anyone else");
            }
        }
    }
}
