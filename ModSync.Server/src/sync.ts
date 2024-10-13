import type { VFS } from "@spt/utils/VFS";
import path from "node:path";
import { hashFile } from "./utility/imoHash";
import type { Config, SyncPath } from "./config";
import { HttpError, winPath } from "./utility/misc";
import { Semaphore } from "./utility/semaphore";
import type { ILogger } from "@spt/models/spt/utils/ILogger";

type ModFile = {
	hash: string;
	directory: boolean;
};

export class SyncUtil {
	private limiter = new Semaphore(1024);

	constructor(
		private vfs: VFS,
		private config: Config,
		private logger: ILogger,
	) {}

	private async getFilesInDir(baseDir: string, dir: string): Promise<string[]> {
		if (!this.vfs.exists(dir)) {
			this.logger.warning(
				`Corter-ModSync: Directory '${dir}' does not exist, will be ignored.`,
			);
			return [];
		}

		const stats = await this.vfs.statPromisify(dir);
		if (stats.isFile()) return [dir];

		const files: string[] = [];
		for (const fileName of this.vfs.getFiles(dir)) {
			const file = path.join(dir, fileName);
			
			if (this.config.isExcluded(file)) continue;

			files.push(file);
		}

		for (const dirName of this.vfs.getDirs(dir)) {
			const subDir = path.join(dir, dirName);

			if (this.config.isExcluded(subDir, baseDir)) continue;
			
			const subFiles = await this.getFilesInDir(baseDir, subDir);
			if (!subFiles.length) files.push(subDir)
			
			files.push(...subFiles);
		}

		if (stats.isDirectory() && files.length === 0) files.push(dir);

		return files;
	}

	private async buildModFile(
		file: string,
		// biome-ignore lint/correctness/noEmptyPattern: <explanation>
		{}: Required<SyncPath>,
	): Promise<ModFile> {
		const stats = await this.vfs.statPromisify(file);
		if (stats.isDirectory()) return { hash: "", directory: true };

		try {
			const lock = await this.limiter.acquire();
			const hash = await hashFile(file);
			lock.release();

			return {
				hash,
				directory: false,
			};
		} catch (e) {
			console.log(e);
			throw new HttpError(500, `Corter-ModSync: Error reading '${file}'\n${e}`);
		}
	}

	public async hashModFiles(
		syncPaths: Config["syncPaths"],
	): Promise<Record<string, Record<string, ModFile>>> {
		const result: Record<string, Record<string, ModFile>> = {};
		const processedFiles = new Set<string>();

		for (const syncPath of syncPaths) {
			const files = await this.getFilesInDir(syncPath.path, syncPath.path);
			const filesResult: Record<string, ModFile> = {};

			for (const file of files) {
				if (processedFiles.has(winPath(file))) continue;
				
				filesResult[winPath(file)] = await this.buildModFile(file, syncPath);

				processedFiles.add(winPath(file));
			}

			result[winPath(syncPath.path)] = filesResult;
		}

		return result;
	}

	/**
	 * @throws {Error} If file path is invalid
	 */
	public sanitizeDownloadPath(
		file: string,
		syncPaths: Config["syncPaths"],
	): string {
		const normalized = path.join(
			path.normalize(file).replace(/^(\.\.(\/|\\|$))+/, ""),
		);
		
		for (const syncPath of syncPaths) {
			const fullPath = path.join(process.cwd(), syncPath.path);
			if (!path.relative(fullPath, normalized).startsWith("..")) {
				return normalized;
			}
		}
		
		throw new HttpError(
			400,
			`Corter-ModSync: Requested file '${file}' is not in an enabled sync path!`,
		);
	}
}
