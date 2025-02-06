using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using Mailo.IRepo;
using Mailo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
namespace Mailo.Controllers
{
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
       
        public ProductController(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }


        [HttpGet]
        public IActionResult _CreateReviewPartial(int id)
        {
            ViewBag.ProductId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> _CreateReviewPartial(ReviewViewModel r)
        {
            if (string.IsNullOrEmpty(r.Content) && !r.Rating.HasValue && (r.clientFile == null || r.clientFile.Length == 0))
            {
                ModelState.AddModelError("", "You must add at least a comment, rating, or photo.");
                ViewBag.ProductId = r.ProductId;
                return RedirectToAction("ProductDetails", "Product", new { id = r.ProductId }); // ✅ الحل الصحيح
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ID == r.ProductId);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Home");
            }

            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            Review review = new Review()
            {
                Content = r.Content,
                UserId = user.ID,
                ProductId = r.ProductId,
                Rating = r.Rating.HasValue ? r.Rating.Value : (int?)null,
                Date = DateTime.Now
            };

            // ✅ التعامل مع رفع الصورة
            if (r.clientFile != null && r.clientFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(r.clientFile.FileName);
                string uploadPath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await r.clientFile.CopyToAsync(stream);
                }

                review.ImageUrl = "/uploads/" + fileName;
            }

            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Review added successfully!";

            return RedirectToAction("ProductDetails", "Product", new { id = r.ProductId }); // ✅ الحل النهائي
        }


        public async Task<IActionResult> Index(int? categoryId, string searchText)
        {
            var query = _context.Products
                 .Include(p => p.Variants)
                 .ThenInclude(v => v.Size)
                 .Include(p => p.Variants)
                 .ThenInclude(v => v.Color)
                 .AsQueryable();
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(p => p.Name.Contains(searchText) || p.Description.Contains(searchText));
            }

            var products = query.ToList();

            ViewBag.Categories = new SelectList(await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync(), "Id", "Name");

            return View(products);
        }
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> ProductManagement()
        {
            return View();
        }
        #region Aya
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Categorys)
                .ToListAsync();

           var cc = await _context.Categories.ToListAsync();
            return View("Index", products);
        }
        #endregion











        [HttpGet]
        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Color)    
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Size)   
                  .Include(p => p.Reviews)
    .ThenInclude(r => r.User) 
    .FirstOrDefaultAsync(p => p.ID == id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.ProductId = id; 


            return View(product);
        }


    }
}












