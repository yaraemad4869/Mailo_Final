using Mailo.Data;
using Mailo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductVariantController : Controller
    {
        private readonly AppDbContext _context;

        public ProductVariantController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ عرض المتغيرات لكل منتج
        public async Task<IActionResult> Index()
        {
            var variants = await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.Color)
                .Include(v => v.Size)
                .ToListAsync();

            return View(variants);
        }

        // ✅ عرض صفحة إضافة متغير جديد
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ID", "Name");
            ViewBag.Colors = await _context.Colors.ToListAsync(); // ✅ اجلب قائمة الألوان
            ViewBag.Sizes = await _context.Sizes.ToListAsync();   // ✅ اجلب قائمة الأحجام
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int ProductId, List<int> ColorIds, List<int> SizeIds, List<int> Quantities)
        {
            if (ProductId == 0 || !ColorIds.Any() || !SizeIds.Any() || !Quantities.Any())
            {
                TempData["Error"] = "Please select at least one color and one size.";
                return RedirectToAction("Create");
            }

            var imagesData = new Dictionary<int, byte[]>();

            // ✅ حفظ الصور مع كل لون
            foreach (var colorId in ColorIds.Distinct())
            {
                var file = Request.Form.Files[$"ImageFiles_{colorId}"];
                if (file != null && file.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        imagesData[colorId] = ms.ToArray();
                    }
                }
                else
                {
                    imagesData[colorId] = null;
                }
            }

            for (int i = 0; i < ColorIds.Count; i++)
            {
                int colorId = ColorIds[i];
                int sizeId = SizeIds[i];

                // ✅ تحقق مما إذا كان `ProductVariant` موجودًا بالفعل
                var existingVariant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.ProductId == ProductId && v.ColorId == colorId && v.SizeId == sizeId);

                if (existingVariant != null)
                {
                    // ✅ إذا كان المنتج موجودًا، قم بتحديث الكمية فقط
                    existingVariant.Quantity += Quantities[i];

                    if (imagesData.ContainsKey(colorId) && imagesData[colorId] != null)
                    {
                        existingVariant.dbImage = imagesData[colorId]; // تحديث الصورة إذا كانت جديدة
                    }
                }
                else
                {
                    // ✅ إذا لم يكن موجودًا، قم بإضافته كمنتج جديد
                    var variant = new ProductVariant
                    {
                        ProductId = ProductId,
                        ColorId = colorId,
                        SizeId = sizeId,
                        Quantity = Quantities[i],
                        dbImage = imagesData.ContainsKey(colorId) ? imagesData[colorId] : null
                    };
                    _context.ProductVariants.Add(variant);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Variants Added/Updated Successfully";
            return RedirectToAction("Index");
        }





        public async Task<IActionResult> Delete(int productId, int colorId, int sizeId)
        {
            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.ProductId == productId && v.ColorId == colorId && v.SizeId == sizeId);

            if (variant == null)
            {
                return NotFound();
            }

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Variant Deleted Successfully";
            return RedirectToAction("Index");
        }

    }
}