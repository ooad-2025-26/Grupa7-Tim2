using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

namespace TimeForPill
{
    public class LjekarsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LjekarsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Ljekars
        public async Task<IActionResult> Index()
        {
            return View(await _context.Ljekari.ToListAsync());
        }

        // GET: Ljekars/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ljekar = await _context.Ljekari
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ljekar == null)
            {
                return NotFound();
            }

            return View(ljekar);
        }

        // GET: Ljekars/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ljekars/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Specijalizacija,Id,Ime,Prezime,Email,Lozinka,DatumRodjenja,Spol")] Ljekar ljekar)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ljekar);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ljekar);
        }

        // GET: Ljekars/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ljekar = await _context.Ljekari.FindAsync(id);
            if (ljekar == null)
            {
                return NotFound();
            }
            return View(ljekar);
        }

        // POST: Ljekars/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Specijalizacija,Id,Ime,Prezime,Email,Lozinka,DatumRodjenja,Spol")] Ljekar ljekar)
        {
            if (id != ljekar.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ljekar);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LjekarExists(ljekar.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ljekar);
        }

        // GET: Ljekars/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ljekar = await _context.Ljekari
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ljekar == null)
            {
                return NotFound();
            }

            return View(ljekar);
        }

        // POST: Ljekars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ljekar = await _context.Ljekari.FindAsync(id);
            if (ljekar != null)
            {
                _context.Ljekari.Remove(ljekar);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LjekarExists(int id)
        {
            return _context.Ljekari.Any(e => e.Id == id);
        }
    }
}
