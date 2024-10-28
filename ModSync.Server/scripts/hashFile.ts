import fs from "node:fs";

import { hashFile } from "../src/utility/imoHash";
import { metrohash128 } from "../src/utility/metroHash";

async function main() {
    if (process.argv.length < 3) {
        console.log("Usage: tsx hashFile.ts <filename>");
        process.exit(1);
    }

    const metroHash = metrohash128(fs.readFileSync(process.argv[2]));
    console.log("MetroHash:", Buffer.from(metroHash).toString("hex"));

    const imoHash = await hashFile(process.argv[2]);
    console.log("ImoHash:", imoHash);
}

main();