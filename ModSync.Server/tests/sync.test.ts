﻿import { expect, describe, it, vi, beforeEach } from "vitest";
import { fs, vol } from "memfs";

import { SyncUtil } from "../src/sync";
import { Config } from "../src/config";
import { VFS } from "./utils/vfs";
import type { VFS as IVFS } from "@spt/utils/VFS";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import { mock } from "vitest-mock-extended";

vi.mock("node:fs", async () => {
	const { readFileSync } = await vi.importActual<typeof import("node:fs")>("node:fs");
	const { fs } = await vi.importActual<typeof import("memfs")>("memfs");

	return {
		...fs,
		readFileSync: vi.fn((path, options) => path.endsWith(".wasm") ? readFileSync("../ModSync.MetroHash/pkg/metrohash_bg.wasm") : fs.readFileSync(path, options)),
	}
});


describe("syncTests", async () => {
	const directoryStructure = {
		plugins: {
			"file1.dll": "Test content",
			"file2.dll": "Test content 2",
			"file2.dll.nosync": "",
			"file3.dll": "Test content 3",
			"file3.dll.nosync.txt": "",
			ModName: {
				IncludedSubdir: {
					"aFileToInclude": "Something",
				},
				"mod_name.dll": "Test content 4",
				".nosync": "",
			},
			OtherMod: {
				"other_mod.dll": "Test content 5",
				sounds: {},
				subdir: {
					"image.png": "Test Image",
					".nosync": "",
				},
			},
		},
		user: {
			mods: {
				testMod: {
					"test.ts": "Test content 6",
					"test.js": "Test content 6",
					"test.js.map": "Test content 7",
				},
			},
		},
	};

	const config = new Config(
		[
			{
				path: "plugins",
				enabled: true,
				enforced: false,
				silent: false,
				restartRequired: true,
			},
		],
		[
			"**/*.nosync",
			"**/*.nosync.txt",
			"plugins/file2.dll",
			"plugins/file3.dll",
			"plugins/ModName",
			"plugins/OtherMod/subdir",
			"user/mods/**/*.js",
			"user/mods/**/*.js.map",
		],
	);

	const vfs = new VFS();
	const logger = mock<ILogger>();
	const syncUtil = new SyncUtil(vfs as IVFS, config, logger);

	beforeEach(() => {
		vol.reset();
		vol.fromNestedJSON(directoryStructure);
	});

	describe("hashModFiles", () => {
		it("should hash mod files", async () => {
			const hashes = await syncUtil.hashModFiles(config.syncPaths);

			expect("plugins\\OtherMod\\other_mod.dll" in hashes.plugins).toBe(
				true,
			);

			expect(hashes).toMatchSnapshot();
		});

		it("should correctly hash multiple folders", async () => {
			const newConfig = new Config(
				[
					{
						path: "plugins",
						enabled: true,
						enforced: false,
						silent: false,
						restartRequired: true,
					},
				],
				config.exclusions,
			);

			const vfs = new VFS();
			const syncUtil = new SyncUtil(vfs as IVFS, newConfig, logger);

			const hashes = await syncUtil.hashModFiles(newConfig.syncPaths);

			expect(hashes).toMatchSnapshot();
		});

		it("previous syncpaths should override later ones", async () => {
			const newConfig = new Config(
				[
					{
						path: "plugins/OtherMod",
						enabled: true,
						enforced: false,
						silent: false,
						restartRequired: true,
					},
					{
						path: "plugins",
						enabled: true,
						enforced: false,
						silent: false,
						restartRequired: true,
					},
				],
				config.exclusions,
			);

			const syncUtil = new SyncUtil(vfs as IVFS, newConfig, logger);

			const hashes = await syncUtil.hashModFiles(newConfig.syncPaths);

			expect(hashes["plugins\\OtherMod"]).toHaveProperty("plugins\\OtherMod\\other_mod.dll");
			expect(hashes.plugins).not.toHaveProperty("plugins\\OtherMod\\other_mod.dll");
		});

		it("should correctly ignore folders that do not exist", async () => {
			const newConfig = new Config(
				[
					{
						path: "plugins",
						enabled: true,
						enforced: false,
						silent: false,
						restartRequired: true,
					},
					{
						path: "user/bananas",
						enabled: true,
						enforced: false,
						silent: false,
						restartRequired: false,
					},
				],
				config.exclusions,
			);

			const syncUtil = new SyncUtil(vfs as IVFS, newConfig, logger);

			const hashes = await syncUtil.hashModFiles(newConfig.syncPaths);

			expect(Object.keys(hashes.plugins)).toContain("plugins\\file1.dll");
			expect(Object.keys(hashes.plugins)).toContain(
				"plugins\\OtherMod\\other_mod.dll",
			);
			expect(logger.warning).toHaveBeenCalledWith(
				"Corter-ModSync: Directory 'user/bananas' does not exist, will be ignored.",
			);
		});

		it("should correctly hash folders that didn't exist initially but are created", async () => {
			const newConfig = new Config(
				[
					{
						path: "plugins",
						enabled: true,
						enforced: false,
						silent: false,
						restartRequired: true,
					},
					{
						path: "user/bananas",
						enabled: true,
						enforced: false,
						silent: false,
						restartRequired: false,
					},
				],
				config.exclusions,
			);

			const syncUtil = new SyncUtil(vfs as IVFS, newConfig, logger);

			const hashes = await syncUtil.hashModFiles(newConfig.syncPaths);

			expect(Object.keys(hashes.plugins)).toContain("plugins\\file1.dll");
			expect(Object.keys(hashes.plugins)).toContain(
				"plugins\\OtherMod\\other_mod.dll",
			);

			expect(logger.warning).toHaveBeenCalledWith(
				"Corter-ModSync: Directory 'user/bananas' does not exist, will be ignored.",
			);

			fs.mkdirSync("user/bananas", { recursive: true });
			fs.writeFileSync("user/bananas/test.txt", "test");

			const newHashes = await syncUtil.hashModFiles(newConfig.syncPaths);

			expect(newHashes).toMatchSnapshot();
		});

		it("should correctly include empty folders", async () => {
			const hashes = await syncUtil.hashModFiles(config.syncPaths);

			expect("plugins\\OtherMod\\sounds" in hashes.plugins).toBe(true);

			expect(hashes).toMatchSnapshot();
		});

		it("should correctly ignore folders with all files excluded", async () => {
			const newConfig = new Config(
				config.syncPaths,
				[
					...config.exclusions,
					"plugins/OtherMod/*",
				]
			);

			const syncUtil = new SyncUtil(vfs as IVFS, newConfig, logger);

			const hashes = await syncUtil.hashModFiles(newConfig.syncPaths);

			expect(hashes).toMatchSnapshot();
		});

		it("Should correctly hash a single file", async () => {
			const hashes = await syncUtil.hashModFiles([
				{
					enabled: true,
					enforced: false,
					path: "plugins/file1.dll",
					restartRequired: true,
					silent: false,
				},
			]);

			expect(hashes).toMatchSnapshot();
		});

		it("Should only hash a given file for one syncPath", async () => {
			const hashes = await syncUtil.hashModFiles([
				{
					enabled: true,
					enforced: false,
					path: "plugins/file1.dll",
					restartRequired: true,
					silent: false,
				},
				{
					enabled: true,
					enforced: false,
					path: "plugins/",
					restartRequired: true,
					silent: false,
				},
			]);

			expect(Object.values(hashes).flatMap(Object.keys).filter((path) => path === "plugins\\file1.dll").length).toBe(1);
			expect(hashes).toMatchSnapshot();
		});

		it("Exclusions can be overridden by more specific syncPaths", async () => {
			const newConfig = new Config(
				[
					...config.syncPaths,
					{
						path: "plugins/ModName/IncludedSubdir",
						enabled: true,
						enforced: false,
						silent: false,
						restartRequired: true,
					}
				],
				config.exclusions,
			)

			const syncUtil = new SyncUtil(vfs as IVFS, newConfig, logger);
			const hashes = await syncUtil.hashModFiles(newConfig.syncPaths);


			expect(hashes["plugins\\ModName\\IncludedSubdir"]).toHaveProperty("plugins\\ModName\\IncludedSubdir\\aFileToInclude");
			expect(hashes).toMatchSnapshot();
		});
	});

	describe("sanitizeDownloadPath", () => {
		it("should sanitize correct download paths", () => {
			expect(
				syncUtil.sanitizeDownloadPath("plugins\\file1.dll", config.syncPaths),
			).toBe("plugins\\file1.dll");
		});

		it("should throw for download paths outside SPT root", () => {
			expect(() => {
				syncUtil.sanitizeDownloadPath(
					"plugins\\..\\file1.dll",
					config.syncPaths,
				);
			}).toThrow();
		});

		it("should throw for files not in syncPath", () => {
			expect(() => {
				syncUtil.sanitizeDownloadPath("otherDir\\file1.dll", config.syncPaths);
			}).toThrow();
		});
	});
});
