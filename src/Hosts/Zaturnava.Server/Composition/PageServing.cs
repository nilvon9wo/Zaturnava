using Microsoft.Extensions.FileProviders;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// The web client's page, served by the game server itself at <see cref="Path"/>, beside itch.io, so that an
/// invitation can be a link that opens a game (Stated 2026-11-19, OQ9: itch.io is for being found): the published
/// page's files, from the folder <c>Page:Folder</c> names, under the server's content root, or anywhere, named in
/// full. None, if it names none, or a folder that is not there.
/// </summary>
internal static class PageServing
{
    public const string FolderKey = "Page:Folder";

    /// <summary>Where the page is served, as the page's invitations link to it.</summary>
    public const string Path = "/play";

    public static WebApplication UsePage(this WebApplication app)
    {
        string root = System.IO.Path.Combine(app.Environment.ContentRootPath, app.Configuration[FolderKey] ?? "-");
        if (app.Configuration[FolderKey] is null || !Directory.Exists(root))
        {
            return app;
        }

        PhysicalFileProvider files = new(root);
        _ = app.Lifetime.ApplicationStopped.Register(files.Dispose);
        _ = app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files, RequestPath = Path });
        _ = app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = files,
                RequestPath = Path,
                // The runtime's own files, whatever they are called, as itch.io serves them.
                ServeUnknownFileTypes = true,
            }
        );
        return app;
    }
}
