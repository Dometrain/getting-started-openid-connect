using Microsoft.AspNetCore.Mvc;
using OidcDemoApp.Infrastructure.BackChannelLogger;

namespace OidcDemoApp.Features.BackChannelLog;

public class BackChannelLogController : Controller
{
    public IActionResult Index()
    {
        return View(BackChannelLogStore.Entries);
    }

    [HttpPost]
    public IActionResult Clear()
    {
        BackChannelLogStore.Clear();
        return RedirectToAction(nameof(Index));
    }
}
