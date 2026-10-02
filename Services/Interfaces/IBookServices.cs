using System.Collections.Generic;
using StudyLink.Data.Entities;
using StudyLink.Services.DTOs;
using StudyLink.Services.Infrastructure;

namespace StudyLink.Services.Interfaces
{
    /// <summary>Public and student-facing catalogue (SC11 / SC12).</summary>
    public interface ICatalogueService
    {
        PagedResult<BookCard> Search(BookFilter filter);
        BookDetail Detail(int bookId, int? campusId, Actor viewer);
        ServiceResult JoinWaitlist(Actor actor, int bookId, int campusId);
        ServiceResult LeaveWaitlist(Actor actor, int waitlistId);
        IList<WaitlistRow> MyWaitlist(Actor actor);

        /// <summary>Cover image bytes for the image handler; null when the book has none.</summary>
        byte[] Cover(int bookId, out string contentType);
    }

    /// <summary>SC25 Book Management and stock control (Manager).</summary>
    public interface IInventoryService
    {
        PagedResult<InventoryRow> Inventory(Actor actor, BookFilter filter);
        BookEdit Get(Actor actor, int bookId);
        /// <summary>
        /// Adds or updates a title. A new title must come with its cover; only the demo seeder and tests
        /// pass <paramref name="allowMissingCover"/> (those books show the placeholder jacket).
        /// </summary>
        ServiceResult<int> Save(Actor actor, BookEdit book, bool allowMissingCover = false);
        ServiceResult SetActive(Actor actor, int bookId, bool active);

        IList<CopyRow> Copies(Actor actor, int bookId);
        ServiceResult<int> AddCopies(Actor actor, int bookId, int campusId, int quantity, CopyCondition condition, string notes);
        ServiceResult UpdateCopy(Actor actor, int copyId, CopyStatus status, CopyCondition condition, string notes);

        IList<CategoryRow> Categories(Actor actor);
        ServiceResult SaveCategory(Actor actor, int? categoryId, string name, string description);
        ServiceResult SetCategoryActive(Actor actor, int categoryId, bool active);
    }

    public interface IRentalService
    {
        /// <summary>SC12 → SC9: reserves one available copy (race-safe) and creates the rental fee.</summary>
        ServiceResult<int> Reserve(Actor actor, int bookId, int campusId);

        /// <summary>Reserves a copy for purchase and creates the purchase fee.</summary>
        ServiceResult<int> Buy(Actor actor, int bookId, int campusId);

        IList<RentalRow> MyRentals(Actor actor);
        IList<SaleRow> MyPurchases(Actor actor);
        RentalRow Get(Actor actor, int rentalId);
        ServiceResult Renew(Actor actor, int rentalId);

        PagedResult<RentalRow> Search(Actor actor, RentalFilter filter);
        RentalRow FindActiveByBarcode(Actor actor, string barcode);
        ServiceResult<ReturnResult> ProcessReturn(Actor actor, ReturnRequest request);
        ServiceResult MarkLost(Actor actor, int rentalId, string notes);

        /// <summary>Background sweep: releases copies reserved for payments that never came.</summary>
        int ExpireHolds();
    }

    public interface IFineService
    {
        IList<FineRow> Search(Actor actor, FineStatus? status, string search);
        ServiceResult Waive(Actor actor, int fineId, string reason);
    }
}
