using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Online.API.Requests.Responses;
using osucc.Plugin;
using UsernameVisuals;

namespace ExamplePlugin
{
    /// <summary>
    /// Demonstrates consuming another plugin's exported API through
    /// <see cref="IOsuCcPluginHost.GetApi{T}"/>. Fetches Username Visuals'
    /// <see cref="IUsernameVisualsApi"/> contract (declared in that plugin's own assembly,
    /// hence the ProjectReference) and — while the <c>username_visuals_integration</c> setting
    /// toggle is on — registers a colour rule plus a display-name rule for the local user, so
    /// the effect is immediately visible in-game. The revoke path is exercised live (toggling
    /// the setting registers/revokes the rules; a temporary rule is also registered and disposed
    /// at startup, with the effective resolution logged before and after) and on shutdown
    /// (<see cref="Dispose"/> revokes the persistent rules). If the <c>username-visuals</c>
    /// plugin is missing or disabled the demo is skipped with a log line; the contract type only
    /// resolves when this class is constructed, in <see cref="ExamplePlugin.AttachToGame"/>
    /// (after every plugin has loaded).
    /// </summary>
    public class ExampleUsernameVisualsApiConsumer : IDisposable
    {
        private readonly IOsuCcPluginHost host;
        private readonly IUsernameVisualsApi? api;
        private readonly Bindable<bool> integrationEnabled;
        private readonly Action changedHandler;

        private IDisposable? colourHandle;
        private IDisposable? nameHandle;
        private bool disposed;

        public ExampleUsernameVisualsApiConsumer(IOsuCcPluginHost host, PluginSettings settings)
        {
            this.host = host;
            api = host.GetApi<IUsernameVisualsApi>("username-visuals");

            // The settings toggle and this consumer share the same live bindable, so flipping it
            // in the settings UI registers or revokes the rules immediately.
            integrationEnabled = settings.Bind("username_visuals_integration", false);
            integrationEnabled.ValueChanged += onIntegrationChanged;

            if (api == null)
            {
                host.Log("username-visuals plugin not available; integration toggle has no effect");
                changedHandler = null!;
                return;
            }

            changedHandler = () => host.Log("username-visuals rules changed");
            api.Changed += changedHandler;

            demonstrateRuleRevocation();

            if (integrationEnabled.Value)
                applyRules();
            else
                host.Log("username-visuals integration disabled; no demo rules registered");
        }

        /// <summary>Registers or revokes the demo colour + name rules to match the toggle.</summary>
        private void onIntegrationChanged(ValueChangedEvent<bool> e) => applyRules();

        private void applyRules()
        {
            if (api == null)
                return;

            if (integrationEnabled.Value)
            {
                if (colourHandle != null)
                    return;

                // A distinctive gradient for the local user, priority 100. The plugin's own
                // own-username display settings (hide, replace) always win; its own palette sits
                // at priority 0, so this gradient beats it while the toggle is on.
                colourHandle = api.AddColourRule(
                    ctx => ctx.User?.OnlineID == ctx.LocalUser?.OnlineID,
                    new[] { new Colour4(0.9f, 0.1f, 0.3f, 1f), new Colour4(0.1f, 0.9f, 0.6f, 1f) },
                    priority: 100);

                // Show the display-name mechanism: replace the local user's name everywhere.
                nameHandle = api.AddNameRule(
                    ctx => ctx.User?.OnlineID == ctx.LocalUser?.OnlineID,
                    UsernameNameRule.Replace("osu-cc demo"),
                    priority: 100);

                host.Log("username-visuals integration enabled: registered demo colour + name rules");
            }
            else
            {
                colourHandle?.Dispose();
                nameHandle?.Dispose();
                colourHandle = null;
                nameHandle = null;

                host.Log("username-visuals integration disabled: demo rules revoked");
            }
        }

        /// <summary>
        /// Proves the register/revoke contract: a temporary rule is registered, the effective
        /// resolution reflects it, and after disposing the returned handle resolution falls back
        /// to the persistent rule again.
        /// </summary>
        private void demonstrateRuleRevocation()
        {
            var user = new APIUser { Id = 12345, Username = "demo" };

            using (var temporary = api!.AddNameRule(_ => true, UsernameNameRule.Replace("temporary"), priority: 300))
                host.Log($"name before revoke: '{api.ResolveName(user, user).Text}'");

            host.Log($"name after revoke: '{api.ResolveName(user, user).Text}'");

            if (api.Enabled)
            {
                using (var temporary = api.AddColourRule(_ => true, new[] { new Colour4(0.2f, 0.3f, 0.9f, 1f) }, priority: 200))
                    host.Log($"colours before revoke: {api.ResolveColour(user, user)?.Count}");

                host.Log($"colours after revoke: {api.ResolveColour(user, user)?.Count}");
            }
            else
            {
                host.Log("colour revoke demo skipped (gradients are disabled)");
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            GC.SuppressFinalize(this);

            integrationEnabled.ValueChanged -= onIntegrationChanged;

            if (api != null)
                api.Changed -= changedHandler;

            colourHandle?.Dispose();
            nameHandle?.Dispose();
        }
    }
}
