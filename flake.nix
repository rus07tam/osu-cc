{
  description = "osu-cc - custom client for osu!lazer";

  inputs.nixpkgs.url = "nixpkgs";

  outputs = { self, nixpkgs }:
    let
      systems = [ "x86_64-linux" ];
      forAllSystems = f: nixpkgs.lib.genAttrs systems (system: f (import nixpkgs { inherit system; }));
    in
    {
      formatter = forAllSystems (pkgs: pkgs.nixfmt);

      devShells = forAllSystems (pkgs:
        let
          sdk = pkgs.dotnet-sdk_8;
        in
        {
          default = pkgs.mkShell {
            packages = [ sdk ];

            env = {
              DOTNET_ROOT = "${sdk}/share/dotnet";
              DOTNET_NOLOGO = "1";
              DOTNET_CLI_TELEMETRY_OPTOUT = "1";
            };

            shellHook = ''
              export DOTNET_CLI_HOME="''${XDG_CACHE_HOME:-$HOME/.cache}/dotnet"
              export NUGET_PACKAGES="''${DOTNET_CLI_HOME}/NuGet/packages"
            '';
          };
        });

      packages = forAllSystems (pkgs:
        let
          inherit (pkgs) dotnet-runtime_8 dotnet-sdk_8;
        in
        {
          default = pkgs.buildDotnetModule {
            pname = "osucc";
            version = "0.1.0";
            src = self;

            projectFile = "osucc.sln";
            nugetDeps = ./packages.lock.json;
            executables = [ "osucc" ];

            dotnet-sdk = dotnet-sdk_8;
            dotnet-runtime = dotnet-runtime_8;

            meta = {
              description = "osu-cc launcher: starts the osu!lazer client with the deployed startup hook (never builds, never touches the install dir)";
              platforms = [ "x86_64-linux" ];
            };
          };
        });

      checks = forAllSystems (pkgs:
        let
          sdk = pkgs.dotnet-sdk_8;
        in
        {
          default = self.packages.${pkgs.system}.default;

          format = pkgs.stdenv.mkDerivation {
            name = "osucc-format-check";
            src = self;
            nativeBuildInputs = [ sdk ];
            buildPhase = ''
              dotnet restore osucc.sln --use-lock-file --locked-mode
              dotnet format osucc.sln --verify-no-changes --no-restore
            '';
            installPhase = "mkdir -p $out";
          };
        });
    };
}
