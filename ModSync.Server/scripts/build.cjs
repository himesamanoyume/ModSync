const { config, rm, mkdir, cp, pushd, popd, exec, sed } = require("shelljs");
const { globSync } = require("glob");
const { readFileSync } = require("node:fs");
const { gzipSync } = require("node:zlib");

const packageJson = require("../package.json");

let configuration = "Release";
if (process.argv.includes("--debug")) configuration = "Debug";

rm("-rf", "../dist");
mkdir("-p", "../dist/user/mods/Corter-ModSync/src");
mkdir("-p", "../dist/BepInEx/plugins");

pushd("-q", "../ModSync.MetroHash");
exec(
	"wasm-pack build --release --target nodejs --out-name metrohash --no-pack --manifest-path Cargo.toml -Z build-std=panic_abort,std -Z build-std-features=optimize_for_size,panic_immediate_abort",
);
const wasmBundle = gzipSync(readFileSync("pkg/metrohash_bg.wasm")).toString(
	"base64",
);
popd("-q");

for (const file of globSync("**/*.ts")) {
	rm(file);
}

sed(
	"-i",
	/.*\/\* WASM Bundle \*\/.*$/gm,
	`/* WASM Bundle */ const zipped = Buffer.from("${wasmBundle}", "base64");`,
	"src/utility/metrohash.ts",
);

cp("package.json", "../dist/user/mods/Corter-ModSync/");
cp("-r", "src/*", "../dist/user/mods/Corter-ModSync/src");

pushd("-q", "../");
exec(`dotnet build -c ${configuration}`);
popd("-q");

pushd("-q", "../ModSync.Updater");
exec(`dotnet publish -c ${configuration} -r win-x64`);
popd("-q");

cp(
	`../ModSync/bin/${configuration}/Corter-ModSync.dll`,
	"../dist/BepInEx/plugins/",
);
cp(
	`../ModSync.Updater/bin/${configuration}/net8.0-windows/win-x64/publish/ModSync.Updater.exe`,
	"../dist/",
);

pushd("-q", "../dist");
config.silent = true;
exec(
	`7z a -tzip Corter-ModSync-v${packageJson.version}.zip BepInEx/ ModSync.Updater.exe user/`,
);
config.silent = false;
popd("-q");
