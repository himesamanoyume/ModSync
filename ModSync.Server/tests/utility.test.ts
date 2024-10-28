import { expect, describe, it } from "vitest";

import * as misc from "../src/utility/misc";

describe("winPath", () => {
	it("should convert unix paths to windows paths", () => {
		expect(misc.winPath("foo/bar/baz")).toBe("foo\\bar\\baz");
	});

	it("should keep windows paths unchanged", () => {
		expect(misc.winPath("foo\\bar\\baz")).toBe("foo\\bar\\baz");
	});
});
