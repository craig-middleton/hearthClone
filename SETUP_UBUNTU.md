**HearthClone setup on Ubuntu 26.10 — 12 November 2026**

This setup supports a visual overhaul of the existing game and card pool using the supplied Desktop assets. Work happens in the local Unity project, with significant changes committed and pushed to GitHub. The Unity plugin is not required for this workflow.

Craig will install Unity and GitHub tools himself. Target the latest stable production Editor: **Unity 6.6, 6000.6.9f1**, released 12 November 2026 and verified against Unity's release API. The existing project still records **6000.5.2f1**; its working copy will need an Editor-managed upgrade, followed by package and scene checks. Do not change ProjectVersion.txt by hand to claim that migration has happened. Unity 6.6 officially lists Ubuntu 22.04 and 24.04; test the Editor on Ubuntu 26.10.

1. **Install Unity yourself using the official download.**

   Use [Unity's downloads](https://unity.com/download) or the [6000.6.9f1 release page](https://unity.com/releases/editor/whats-new/6000.6.9f1). Craig is handling this installation. The previously downloaded portable Hub and its extracted files were deleted at Craig's request. Use your own Unity installation.

2. **Sign in and activate your Unity license.**

   Sign in to your Unity account in Hub. Under the Settings gear, open Licenses and check for an active license. Personal licenses normally activate on sign-in. If no license appears and you qualify for Personal under Unity's terms, use Licenses > Add license > Get a free personal license. Read and accept the terms yourself. Keep the license active in Hub before opening the Editor.

3. **Select the latest stable Editor.**

   In Hub, open Installs > Install Editor and select **6000.6.9f1** (Unity 6.6). If Hub's list has not refreshed, use [the official release page](https://unity.com/releases/editor/whats-new/6000.6.9f1). This is the latest production release verified on 12 November 2026; alpha and beta releases are separate streams.

   Record your installation directory and wait for installation to complete. The working project will be migrated from its recorded 6000.5.2f1 version after the new Editor is available. Preserve that migration in its own Git checkpoint and check packages, compilation and scene appearance afterward.

4. **Keep the module selection small.**

   Install the Linux Editor. For this project's initial Linux desktop workflow, its native Mono support is sufficient. Linux IL2CPP, Android, iOS, Web and Windows build modules can wait until those platforms are needed. Correction to the earlier chat: Linux Build Support (Mono) need not appear as a separate checkbox on the Linux installer.

5. **Install your normal GitHub tools.**

   Craig is handling these installations. The command-line workflow uses Git, [GitHub CLI (`gh`)](https://cli.github.com/), and [Git LFS](https://git-lfs.com/). GitHub CLI handles account authentication and GitHub operations; Git LFS handles artwork and audio stored outside ordinary Git blobs. A desktop GitHub application is optional.

   Verify your installations in a normal terminal:

   ```bash
   git --version
   gh --version
   git lfs version
   ```

   Each command should print a version. The portable GitHub CLI and Git LFS copies, their archives, and their extracted files were deleted at Craig's request. They had not replaced system installations. The game's already-retrieved LFS assets remain in the working project; use your normal tool installations for future Git operations.

6. **Authenticate GitHub for pushes.**

   Run:

   ```bash
   gh auth login --hostname github.com --git-protocol https --web
   ```

   Follow the browser login flow using the GitHub account that owns `craig-middleton/hearthClone`. If asked whether to authenticate Git with the same credentials, answer Yes. Enter any device code on GitHub's authentication page yourself.

   Then run:

   ```bash
   gh auth setup-git --hostname github.com
   gh auth status
   ```

   The status should show your account logged in. Authentication belongs in GitHub CLI and the browser; no password or access token needs to be pasted into this chat.

7. **Prepare the existing working copy and retrieve its LFS assets.**

   The project has already been copied to:

   ```text
   /home/craig-middleton/Documents/Codex/2026-11-12/if-x20/outputs/HearthClone
   ```

   Its remote is `https://github.com/craig-middleton/hearthClone.git`, and its working branch is `main`. Use that copy consistently so the Editor and this chat operate on the same files. Do not create a new template project over this folder.

   In the same terminal, run these idempotent checks:

   ```bash
   cd "/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/outputs/HearthClone"
   git lfs install --local
   git lfs pull
   git lfs fsck
   git branch --show-current
   git remote -v
   ```

   LFS supplies the artwork and audio stored as pointer files in an ordinary clone. These assets have already been retrieved and `git lfs fsck` passed during setup preparation; the commands above repeat the checks safely. A Linux-specific `.gitattributes` fix also covers the uppercase `.TTF` sample font. A failed pull needs resolving before judging missing visuals in Unity. The large extracted Desktop library is separate from the repository's existing LFS assets; leave those source folders in place.

8. **Check the commit identity.**

   Inside the working project, inspect:

   ```bash
   git config user.name
   git config user.email
   ```

   Commits use the same author identity as the previous project commits. You can change this repository's identity if you prefer a different commit email, including your GitHub-provided private commit address:

   ```bash
   git config --local user.name "Craig Middleton"
   git config --local user.email "YOUR_CHOSEN_COMMIT_EMAIL"
   ```

   Replace the example email before running the second command. This affects this project only.

9. **Register the project in Unity Hub.**

   In Hub's Projects view, choose Add / Add project from disk and select the `HearthClone` folder above. Select the folder containing `Assets`, `Packages`, and `ProjectSettings`. Associate it with **6000.6.9f1**. You can leave the game closed while the revised visuals are being prepared.

   For an installation-only check without opening the existing game, create a separate empty 2D project named `UnityInstallCheck` under:

   ```text
   /home/craig-middleton/Documents/Codex/2026-11-12/if-x20/work/UnityInstallCheck
   ```

   Wait for the Editor to finish importing, confirm the empty scene is visible, and close it. This verifies that the Editor launches on Ubuntu 26.10. The test project is disposable and does not belong in the HearthClone repository.

10. **Use the agreed visual-work workflow.**

    Continue in this chat. The implementation will use the existing game, selected supplied assets, and Unity Editor scripts to apply scene and prefab changes. A separate styled scene will keep the original scene available for comparison. The visual overhaul has not yet been implemented.

    Once the revised scene is ready, open the working project with the matching Editor, allow package import and script compilation to complete, and follow the provided Editor-tool instructions. Import-time compilation is Unity's normal preparation; there is no need to export a standalone build of the original game. Save relevant scene and prefab edits before each checkpoint. Avoid externally editing scene YAML while Unity has an unsaved in-memory copy of that scene.

    Significant changes will receive focused commits on `main`, with pushes to GitHub once authentication works. Validation notes will distinguish source checks from Editor and playtest results. A commit is a local checkpoint; it is only backed up to GitHub after a successful push. Editor caches, logs, generated project files, credentials and the entire uncurated Desktop export do not belong in those commits.

Setup is ready when Hub opens, a license is active, Editor 6000.6.9f1 launches, GitHub authentication succeeds, and the repository's LFS assets are present. Tell me which step you reach or the exact error that stops you, and we can continue from there.

Sources: [Unity Linux installation](https://docs.unity.com/en-us/hub/install-hub-linux), [Unity 6.6 requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements), [Unity licenses](https://docs.unity.com/en-us/hub/manage-license), [Editor installation](https://docs.unity.com/en-us/hub/add-editor), [GitHub CLI authentication](https://cli.github.com/manual/gh_auth_login), [Git credential setup](https://cli.github.com/manual/gh_auth_setup-git), [Git LFS](https://git-lfs.com/).
