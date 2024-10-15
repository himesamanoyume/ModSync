import * as fs from "fs";
import * as util from "util";
import { metrohash128 } from "./metroHash";

const SAMPLE_THRESHOLD = 10 * 1024 * 1024;
const SAMPLE_SIZE = 32 * 1024;

// Promisify fs functions
const fsOpen = util.promisify(fs.open);
const fsClose = util.promisify(fs.close);
const fsFstat = util.promisify(fs.fstat);
const fsRead = util.promisify(fs.read);

// Placeholder functions for mmh3 and varint (same as before)

function putUvarint(buf: Uint8Array, x: number): number {
	let i = 0;
	while (x >= 0x80) {
		buf[i] = (x & 0xff) | 0x80;
		x >>= 7;
		i++;
	}
	buf[i] = x & 0xff;
	return i + 1;
}

async function readChunk(
	fd: number,
	position: number,
	length: number,
): Promise<Buffer> {
	const buffer = Buffer.alloc(length);
	const { bytesRead } = await fsRead(fd, buffer, 0, length, position);
	if (bytesRead < length) {
		throw new Error("Could not read enough data");
	}
	return buffer;
}

async function hashFileObject(
	fd: number,
	sampleThreshold: number = SAMPLE_THRESHOLD,
	sampleSize: number = SAMPLE_SIZE,
): Promise<string> {
	const stats = await fsFstat(fd);
	const size = stats.size;

	let data: Buffer;

	if (size < sampleThreshold || sampleSize < 1 || size < 4 * sampleSize) {
		data = await readChunk(fd, 0, size);
	} else {
		const start = await readChunk(fd, 0, sampleSize);
		const middle = await readChunk(fd, Math.floor((size - sampleSize) / 2), sampleSize);
		const end = await readChunk(fd, size - sampleSize, sampleSize);
		data = Buffer.concat([start, middle, end]);
	}

	const hashTmp = metrohash128(data);

	putUvarint(hashTmp, size);

	return Buffer.from(
		hashTmp.buffer,
		hashTmp.byteOffset,
		hashTmp.byteLength,
	).toString("hex");
}

export async function hashFile(
	filename: string,
	sampleThreshold: number = SAMPLE_THRESHOLD,
	sampleSize: number = SAMPLE_SIZE,
): Promise<string> {
	let fd: number | null = null;
	try {
		fd = await fsOpen(filename, "r");
		return await hashFileObject(fd, sampleThreshold, sampleSize);
	} finally {
		if (fd !== null) {
			await fsClose(fd);
		}
	}
}
