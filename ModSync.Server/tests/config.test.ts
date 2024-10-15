import { expect, beforeEach, describe, it, vi } from "vitest";

import { Config, ConfigUtil } from "../src/config";
import { VFS } from "./utils/vfs";
import { JsonUtil } from "./utils/jsonUtil";
import { PreSptModLoader } from "./utils/preSptModLoader";
import type { VFS as IVFS } from "@spt/utils/VFS";
import type { JsonUtil as IJsonUtil } from "@spt/utils/JsonUtil";
import type { PreSptModLoader as IPreSptModLoader } from "@spt/loaders/PreSptModLoader";
import { fs, vol } from "memfs";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import { mock } from "vitest-mock-extended";
import path from "node:path";

vi.mock("node:fs", async () => (await vi.importActual("memfs")).fs);

describe("Config", () => {
	let config: Config;
	beforeEach(() => {
		config = new Config(
			[
				{
					path: "plugins",
					enabled: true,
					enforced: false,
					silent: false,
					restartRequired: true,
				},
				{
					path: "mods",
					enabled: false,
					enforced: false,
					silent: false,
					restartRequired: false,
				},
			],
			["plugins/**/node_modules", "plugins/**/*.js"],
		);
	});

	it("should correctly identify excluded paths", () => {
	
		expect(config.isExcluded("plugins/test.dll")).toBe(false);
		expect(config.isExcluded("plugins/banana/node_modules")).toBe(true);
		expect(config.isExcluded("plugins/banana/test.js")).toBe(true);
		expect(config.isExcluded("plugins/banana/config.json")).toBe(false);
		expect(config.isExcluded("plugins/banana/node_modules/lodash")).toBe(false);
	});
});

describe("ConfigUtil", () => {
	beforeEach(() => {
		vol.reset();
	});

	it("should create config if none exists", async () => {
		vol.fromNestedJSON(
			{ "placeholder.txt": "to ensure directory exists" },
			process.cwd(),
		);

		const config = await new ConfigUtil(
			new VFS() as IVFS,
			new JsonUtil() as IJsonUtil,
			new PreSptModLoader() as IPreSptModLoader,
			mock<ILogger>(),
		).load();

		expect(fs.existsSync(path.join(process.cwd(), "config.jsonc"))).toBe(true);
	});

	it("should load config", async () => {
		vol.fromNestedJSON(
			{
				"config.jsonc": `{
				"syncPaths": [
					"plugins",
					{ "path": "mods", "enabled": false },
					{ "path": "doesnotexist", "enabled": true }
				],
				// Exclusions for commonly used SPT mods
				"exclusions": [
					"plugins/**/node_modules"
				]
			}`,
				plugins: {},
				mods: {},
			},
			process.cwd(),
		);

		const config = await new ConfigUtil(
			new VFS() as IVFS,
			new JsonUtil() as IJsonUtil,
			new PreSptModLoader() as IPreSptModLoader,
			mock<ILogger>(),
		).load();

		expect(config.syncPaths).toEqual([
			{
				enabled: true,
				enforced: false,
				path: "doesnotexist",
				restartRequired: true,
				silent: false,
			},
			{
				enabled: true,
				enforced: false,
				path: "plugins",
				restartRequired: true,
				silent: false,
			},
			{
				enabled: false,
				enforced: false,
				path: "mods",
				restartRequired: true,
				silent: false,
			},
		]);

		expect(config.exclusions).toEqual(["plugins/**/node_modules"]);
	});

	it("should reject syncPath object without path", () => {
		vol.fromNestedJSON(
			{
				"config.jsonc": `{
					"syncPaths": [
						{ "enabled": true, "enforced": false, "path": "plugins", "restartRequired": true, "silent": false },
						{ "enabled": false, "enforced": false, "path": "mods", "restartRequired": true, "silent": false },
						{ "enabled": true, "enforced": false, "restartRequired": true, "silent": false }
					],
					// Exclusions for commonly used SPT mods
					"exclusions": [
						"plugins/**/node_modules"
					]
				}`,
			},
			process.cwd(),
		);

		const configUtil = new ConfigUtil(
			new VFS() as IVFS,
			new JsonUtil() as IJsonUtil,
			new PreSptModLoader() as IPreSptModLoader,
			mock<ILogger>(),
		);

		expect(configUtil.load()).rejects.toThrowErrorMatchingSnapshot();
	});

	it("should reject absolute paths", () => {
		vol.fromNestedJSON(
			{
				"config.jsonc": `{
					"syncPaths": [
						"/etc/shadow",
						"C:\\\\Windows\\\\System32\\\\cmd.exe"
					],
					// Exclusions for commonly used SPT mods
					"exclusions": [
						"plugins/**/node_modules"
					]
				}`,
			},
			process.cwd(),
		);

		const configUtil = new ConfigUtil(
			new VFS() as IVFS,
			new JsonUtil() as IJsonUtil,
			new PreSptModLoader() as IPreSptModLoader,
			mock<ILogger>(),
		);

		expect(configUtil.load()).rejects.toThrowErrorMatchingSnapshot();
	});

	it("should reject paths outside of SPT root", () => {
		vol.fromNestedJSON(
			{
				"config.jsonc": `{
					"syncPaths": [
						"../../etc/shadow"
					],
					// Exclusions for commonly used SPT mods
					"exclusions": [
						"plugins/**/node_modules"
					]
				}`,
			},
			process.cwd(),
		);

		const configUtil = new ConfigUtil(
			new VFS() as IVFS,
			new JsonUtil() as IJsonUtil,
			new PreSptModLoader() as IPreSptModLoader,
			mock<ILogger>(),
		);

		expect(configUtil.load()).rejects.toThrowErrorMatchingSnapshot();
	});

	it("should reject non-array syncPaths", () => {
		vol.fromNestedJSON(
			{
				"config.jsonc": `{
					"syncPaths": "plugins",
					// Exclusions for commonly used SPT mods
					"exclusions": [
						"plugins/**/node_modules"
					]
				}`,
			},
			process.cwd(),
		);

		const configUtil = new ConfigUtil(
			new VFS() as IVFS,
			new JsonUtil() as IJsonUtil,
			new PreSptModLoader() as IPreSptModLoader,
			mock<ILogger>(),
		);

		expect(configUtil.load()).rejects.toThrowErrorMatchingSnapshot();
	});

	it("should reject on non-array exclusions", () => {
		vol.fromNestedJSON(
			{
				"config.jsonc": `{
					"syncPaths": [
						"plugins"
					],
					// Exclusions for commonly used SPT mods
					"exclusions": "plugins/**/node_modules"
				}`,
			},
			process.cwd(),
		);

		const configUtil = new ConfigUtil(
			new VFS() as IVFS,
			new JsonUtil() as IJsonUtil,
			new PreSptModLoader() as IPreSptModLoader,
			mock<ILogger>(),
		);

		expect(configUtil.load()).rejects.toThrowErrorMatchingSnapshot();
	});

	it("should reject invalid JSON", () => {
		vol.fromNestedJSON(
			{
				"config.jsonc": `{
					"invalid": "invalid"
				}`,
			},
			process.cwd(),
		);

		const configUtil = new ConfigUtil(
			new VFS() as IVFS,
			new JsonUtil() as IJsonUtil,
			new PreSptModLoader() as IPreSptModLoader,
			mock<ILogger>(),
		);

		expect(configUtil.load()).rejects.toThrowErrorMatchingSnapshot();
	});
});
