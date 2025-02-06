using Mailo.Data;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        public CategoriesController(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            return View(await _unitOfWork.categories.GetAll());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
               
                var exists = _context.Categories.Any(c => c.Name == category.Name);

                if (exists)
                {
                    ModelState.AddModelError("Name", "The category name already exists.");
                    return View(category);
                }
                _unitOfWork.categories.Insert(category);
                return RedirectToAction("Index");
            }

            return View(category);
        }
        public async Task<IActionResult> Edit(int id)
        {
            return View(await _unitOfWork.categories.GetByID(id));
        }
        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {

                var exists = _context.Categories.Any(c => c.Name == category.Name && c.Id != category.Id);

                if (exists)
                {
                    ModelState.AddModelError("Name", "The category name already exists.");
                    return View(category);
                }
                _unitOfWork.categories.Update(category);
                return RedirectToAction("Index");
            }

            return View(category);
        }

    }
}
