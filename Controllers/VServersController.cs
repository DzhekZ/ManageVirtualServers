using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ManageVirtualServers.DAL;

namespace ManageVirtualServers.Controllers
{
    public class VServersController : Controller
    {
        private VServers db = new VServers();

        // GET: VServers
        public async Task<ActionResult> Index()
        {
            return RedirectToAction("Manage");
            return View(await db.VirtualServers.ToListAsync());
        }

        // GET: VServers/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VirtualServer virtualServer = await db.VirtualServers.FindAsync(id);
            if (virtualServer == null)
            {
                return HttpNotFound();
            }
            return View(virtualServer);
        }

        // GET: VServers/Create
        public async Task<ActionResult> Create()
        {
            bool testAny = await db.VirtualServers.AnyAsync();
            int maxId = 0;
            if (testAny)
            {
                maxId = await db.VirtualServers.MaxAsync(x => x.VirtualServerId);
            }
            VirtualServer virtualServer = new VirtualServer
            {
                VirtualServerId = maxId + 1,
                CreateDateTime = DateTime.Now
            };
            return View(virtualServer);
        }

        // POST: VServers/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "VirtualServerId,CreateDateTime,RemoveDateTime,Seq")] VirtualServer virtualServer)
        {
            if (ModelState.IsValid)
            {
                db.VirtualServers.Add(virtualServer);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(virtualServer);
        }

        // GET: VServers/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VirtualServer virtualServer = await db.VirtualServers.FindAsync(id);
            if (virtualServer == null)
            {
                return HttpNotFound();
            }
            return View(virtualServer);
        }

        // POST: VServers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "VirtualServerId,CreateDateTime,RemoveDateTime,Seq")] VirtualServer virtualServer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(virtualServer).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(virtualServer);
        }

        // GET: VServers/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            VirtualServer virtualServer = await db.VirtualServers.FindAsync(id);
            if (virtualServer == null)
            {
                return HttpNotFound();
            }
            return View(virtualServer);
        }

        // POST: VServers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            VirtualServer virtualServer = await db.VirtualServers.FindAsync(id);
            db.VirtualServers.Remove(virtualServer);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // GET: VServers
        public async Task<ActionResult> Manage()
        {
            DateTime currDate = new DateTime(2019, 6, 23, 16, 51, 0);

            ViewBag.CTime = currDate.ToString("yyyy-MM-dd HH:mm:ss");
            ViewBag.TUTime = CalcTotalUsageTime(currDate).ToString(@"hh\:mm\:ss");

            return View(await db.VirtualServers.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(FormCollection formCollection)
        {
            if (ModelState.IsValid)
            {
                var valuesList = formCollection["item.CheckedServer.Value"];
                if (!string.IsNullOrEmpty(valuesList))
                {
                    var cbItems = valuesList.Split(',');
                    if (cbItems != null && cbItems.Any())
                    {
                        foreach (var item in cbItems)
                        {
                            if (string.IsNullOrEmpty(item) || item == "false") continue;

                            VirtualServer virtualServer = await db.VirtualServers.FindAsync(int.Parse(item));

                            db.Entry(virtualServer).State = EntityState.Modified;
                            virtualServer.RemoveDateTime = DateTime.Now;
                        }
                        await db.SaveChangesAsync();
                    }
                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Manage");
        }

        private TimeSpan CalcTotalUsageTime(DateTime currentDateTime)
        {
            TimeSpan returnValue = TimeSpan.Zero;

            var vServers = db.VirtualServers.ToList();
            if (vServers != null && vServers.Any())
            {
                vServers = vServers.OrderBy(x => x.CreateDateTime).ToList();
                //инициализируем значениями из первой строки
                DateTime minBlock = vServers.First().CreateDateTime.Value;
                DateTime maxBlock = vServers.First().RemoveDateTime ?? currentDateTime;

                if (vServers.Count > 1)
                {
                    vServers = vServers.Where(x => x.CreateDateTime > minBlock).ToList();

                    foreach (var item in vServers)
                    {
                        if (item.CreateDateTime > maxBlock)
                        {
                            //новый блок
                            returnValue += maxBlock.Subtract(minBlock);
                            minBlock = item.CreateDateTime.Value;
                            maxBlock = item.RemoveDateTime ?? currentDateTime;
                        }
                        else
                        {
                            var tempMaxBlock = item.RemoveDateTime ?? currentDateTime;
                            if (tempMaxBlock > maxBlock)
                            {
                                maxBlock = tempMaxBlock;
                            }
                        }
                    }

                    returnValue += maxBlock.Subtract(minBlock);
                }
            }

            return returnValue;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
