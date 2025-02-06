using Mailo.Data;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Controllers
{
    public class WishlistController : Controller
    {
        private readonly IAddToWishlistRepo _wishlist;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _db;
        public WishlistController(AppDbContext db, IAddToWishlistRepo wishlist, IUnitOfWork unitOfWork)
        {
            _wishlist = wishlist;
            _unitOfWork = unitOfWork;
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            User? user = _db.Users.Where(x => x.Email == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {

                var wishlistItems = await _db.Wishlists
    .Where(w => w.UserID == user.ID)
    .Include(w => w.product)
        .ThenInclude(p => p.Variants)
            .ThenInclude(v => v.Size)
    .Include(w => w.product)
        .ThenInclude(p => p.Variants) 
            .ThenInclude(v => v.Color) 
    .Include(w => w.product) 
        .ThenInclude(p => p.OrderProducts)
            .ThenInclude(op => op.order)
                .ThenInclude(o => o.user) 
    .Select(w => w.product) 
    .ToListAsync();
                if (wishlistItems == null || wishlistItems.Count == 0 || !wishlistItems.Any())
                {
                    return View();
                }
                return View(wishlistItems);
            }
            else
            {
                return RedirectToAction("Login", "Account");

            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearWishlist()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user != null)
            {
                var wishlistP = await _db.Wishlists.Where(w => w.UserID == user.ID).ToListAsync();
                if (wishlistP != null)
                {
                    _db.Wishlists.RemoveRange(wishlistP);
                    return RedirectToAction("Index");

                }
                else
                {
                    return View("Index");

                }
            }
            else
            {
                return RedirectToAction("Login", "Account");

            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddtoWishlist(Product product)
        {

            User? user = _db.Users.Where(x => x.Email == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                var productwishlist = await _unitOfWork.products.GetByID(product.ID);

                if (productwishlist != null)
                {
                    var existingWishlistEntry = await _db.Wishlists.FirstOrDefaultAsync(w => w.UserID == user.ID && w.ProductID == product.ID);

                    if (existingWishlistEntry == null)
                    {
                        var wishlistEntry = new Wishlist
                        {
                            UserID = user.ID,
                            ProductID = product.ID,
                            AdditionDate = DateTime.Now
                        };

                        _db.Wishlists.Add(wishlistEntry);
                        await _unitOfWork.CommitChangesAsync();
                        TempData["SuccessMessage"] = "Product added to wishlist successfully.";
                    }
                    else
                    {
                        TempData["WarningMessage"] = "The product is already in the wishlist.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Product not found.";

                }
                return RedirectToAction("Index");

            }
            return RedirectToAction("Login", "Account");

        }



        [HttpPost]
        public async Task<IActionResult> RemoveFromWishlist(Product product)
        {


            User? user = _db.Users.Where(x => x.Email == User.Identity.Name).FirstOrDefault();
            if (user != null)
            {
                var productwishlist = await _unitOfWork.products.GetByID(product.ID);
                if (productwishlist != null)
                {
                    var wishlistEntry = await _db.Wishlists.FirstOrDefaultAsync(w => w.UserID == user.ID && w.ProductID == product.ID);

                    if (wishlistEntry != null)
                    {
                        _db.Wishlists.Remove(wishlistEntry);
                        await _db.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Product removed from wishlist successfully.";
                    }
                    else
                    {
                        TempData["WarningMessage"] = "The product is already in the wishlist.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Product not found.";

                }


            }

            return RedirectToAction("Index");

        }
    }
}
