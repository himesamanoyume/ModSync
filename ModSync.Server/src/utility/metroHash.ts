import { gunzipSync } from "node:zlib";


const heap = new Array(128).fill(undefined);

heap.push(undefined, null, true, false);

function getObject(idx: number) { return heap[idx]; }

let heap_next = heap.length;

function dropObject(idx: number) {
	if (idx < 132) return;
	heap[idx] = heap_next;
	heap_next = idx;
}

function takeObject(idx: number) {
	const ret = getObject(idx);
	dropObject(idx);
	return ret;
}

function addHeapObject(obj: Uint8Array) {
	if (heap_next === heap.length) heap.push(heap.length + 1);
	const idx = heap_next;
	heap_next = heap[idx];

	heap[idx] = obj;
	return idx;
}

let cachedUint8ArrayMemory0: Uint8Array | null = null;

function getUint8ArrayMemory0() {
	if (cachedUint8ArrayMemory0 === null || cachedUint8ArrayMemory0.byteLength === 0) {
		cachedUint8ArrayMemory0 = new Uint8Array(wasm.memory.buffer);
	}
	return cachedUint8ArrayMemory0;
}

let WASM_VECTOR_LEN = 0;

function passArray8ToWasm0(arg: Uint8Array, malloc: (size: number, count: number) => number) {
	const ptr = malloc(arg.length, 1) >>> 0;
	getUint8ArrayMemory0().set(arg, ptr);
	WASM_VECTOR_LEN = arg.length;
	return ptr;
}

/**
 * @param {Uint8Array} data
 * @returns {Uint8Array}
 */
export function metrohash128(data: Uint8Array): Uint8Array {
	const ptr0 = passArray8ToWasm0(data, wasm.__wbindgen_export_0);
	const len0 = WASM_VECTOR_LEN;
	const ret = wasm.metrohash128(ptr0, len0);
	return takeObject(ret);
}

// biome-ignore format: Automatically generated via ModSync.MetroHash
/* WASM Bundle */ const zipped = Buffer.from("H4sIAAAAAAAACrU6bYhcWVbnfryq132rum66q7o7qWRy3k2H7SiZycemO6My1G3oxCY0vb9F6HS6Kx+vknSqutLJYJOquAlOcJABVxxhdQcG8Y8iqKAoQgv+WBH9oywsK5phlRXZHyP4MbqzjHPOfa+6OunZGRakSb2Pc9/5Pueec25gbeuOAABxKr4qeqJ3VfZ6cFX0gG5E76rq0TP9it5VoH8g35PR9Orqg2u37m7caN5dvXd7bb15c/P2RrOzunpkCLB5LW2ud1c3Opv3VjvN6yA/86tjBLixeu3+9evNzur6+lpz48LZtbnzFzYurp3bAPjMD8+GD+82Hzy41b157c1uc/P69a1md+3uxu3m3Rvdm6vzzfPNa/MX58+dubh2ff78Gfgc3hnZ6vXm+rm5s2eb114/e/HM9dcv/AgWDg0B7jTvbHbehIJyCUgphFBKQFGAFkILIQCEBAANSgFESsChwojoCd/v74IpvqYK4XsJpTvNbmfz5trWzbPnLoKZGCLRfHhvs9NdPQOl0advJL9l5WhPPJIN+kPw/ylWNF3/5pNfWClLBG9S/7CdgP/4q7swK2GxLDwgtBIdPrBjruhl1z/v70I6K8FJ+hyL9kg3iRUtkbNS+4dtpxCWyoAKoeUU6pUyoEwEKqfL4CHRJW0wotVVp1ChxNgf2/a6nXobEF+qY4SqnkQYe9FNYpRlMBjpBkaJLCmDoiyMh0R4gcWufSVnue3kIhG7OcxnIku8WBrVQOJZoKR/GbOCuCyiXnJRHQHFFafqiUCNRdQY1YkNjdIeYzSMRzZQEBnw/SdM1qll1CzuSrsMKBCsSyQKW5QN1N6uMFHwqjMnNUrUts6/kyVhCIA6tVMGhZ1JpEFZZoY/CiJ5GyzzkWrTzVLdgVfbTmw76VVbN1D63pYXbRSpE151nQyCO/DviBTBvytYp4DCjiOgtFMIdqZi9thfJpUt6saw4bPbfYp0IqiMfQJFIlUDhT3mBIk3AJHFUZE+WXOkbFUPykPJmhEohzUjB5oRrBn6rZJm5EAz0s6QZUVJGi9QdO0rZMJu+6ZTJLLIRJYsssxFjlDZcYwG6COi7sj20HLANgBbxYi0MTClRFgpk7Ge5C9WdMNDEp0Ab1sungEEJ/3v93Xq+/2d9qd6ttsNSBTGXpMJYE7GyJHDcFS+d8lBneXzgMrbLtIjyAbGs1K7AvlSPCtLSdG/82SX72M3wvezElIn5iT4rz3ZDT+ZMdlVA5rhP/92WKIb/vnjXXA6EYrdVaGwCRYulSXZKk5ECQwY//Uc4yICFq50yroUGVpRckBeBV5sY/Fyh9yaPT0n4GBZ91Cx4VOEK5EHQ0FJCPewYoHQLhGfWMCRNAHVQL1YZtOyS13WDbqLE80eol+mTLFYIASaoBpHUtJlwR5PSKbjlFhQWpdEdB14kkSVtihW93SCTEz4t8LjpbJg7img2l5cyoL0YduBnc6CmNQsU4rKiDzO1uhqJ8kPCSlGZBrylNxdUKYOiFlyO0G8UCy/NVhLbpb72vBaWzeo7EwSsfqZryBozvyAwEhqyyVlBnYYtYds4pR/rZUAjlJs28NI8Y+Ao96mS3Wn7UwCqP00S1TAET/bsmXUnryS/eSUhIQyQpz6fw2P85IQmHkJ/vuPdwGLxP0/090I3dFHbBVejyQG+ZnX1qHwxTmpnfA6RbVEmVvjKKl+lG5aLrDpbI2vk4a0SR7D+Ix/j1T0yfG5z6UMwbd9Xwa7hiSgKPw/DbN3RKZd5b9OiZBuhY/TRLCj5Vowg0AHlMvloGeyeivEHal5oGt2tICVkLF9h7yODWgw9jadAYwqATelmYORcToiZHIIGQwho92gYnKHTZRsZMhC5lRho2F/QibAjpajtPU9dLkXUnrKvDAohzYEdj3zd0qoHoI9Sa6Tb46EP88t/NYpL2iTU162FylRBlFEyrGi7AkHgwDTWUmg2l5d/rSaGPBJ2kNpa7QLobLTRublg2O8vK29nYepRLkXsxSHiliTdjqgSR1JOqC6jxATYFKGHWWJM4+wI8HL3stiS7Qyo5aFrVSy2IaXYluEXLxn+5wmkqx7Wt3TtPHv5t+ulGEvFTlYLA/w+tkrVEnZQ0eMrQxevjvYnPnW9widCQLZ0cwrEF5KKkNM2rr5QymiHhlpmryHbEgPpUtc+VWpuKAqTNRD3UUVX0K7LL2OaS+kdCzmZImUOCdj2pbBV6musCmKeqIpnyeR7KGk8qDKRaHw1ZRLm1lpqQ7yNjVURHERF/QiG6gWSSOBoaNODioNRxsDRbTy1ldRzUqLcKmeBs8Ri1weoc4ey6FezQsW/wjlg/achFBaqTk5Tfitywq/OWkpNObktAmikyEo2c/JKkIAmL+WQvZYgZn3/z97/hf1ffVj+z7JsrdrfQHPfhnnC579RR3QfE8I3UNYgHmyIwo75vScPIp6YHDSsReou04NCvnFUBgGPHJOTu+zMlJOYxNLzotcMx7pJhHtB6hcVttnZogd27cUEMbIAkwHvKVg9LhCnnlsO1EYUccRskzoSNgfnQr72BBHpFUIGAitWSOnEf5h23/4OEjVy4t4ch0UXm13nSDR+D1x2uFUnQkSGyqNA48iRz3Eo/nNw3K8px8NKtET4LHlRmYAR2jBURxBloyfqmHZ2AxXiDji49SN44ivpm6UdAI4OithSfdwjHZF60VS9iI5JBsM4Kp+dFbqhEp1IHvQmyI5iEXqg8AL3kvpnbWyR6mDlrpyaClB9vLm5WHWuyQSi/akM9T2ZTuMti4pUDY4DZobmuFqlrokyo2FvYAQWBgOiALFTcnLdrmAJcJXQp06jXKpXMACluw0at7LvaXcHypF3rPtBOWQ7Demsmtvof4RC/WgYsgoXSnLUsFQy2MnKMT6+6BLZU2tTkBdWeZdW9sJbl4hKZD+QlVR4HSTUyzw9h0qXv+oPSd1Fm+FPN44xgefZ1XwBEUC0ZLZBvUkLwhC5NM3BlVCybEkDaVt35ehllCDhlh6nS5R6mj5x/14pbNYFkcIDEeMB8PdtAldn53hzcJGjrI5FskD/I5/iIbsQh1qHaV/2E5pLyFvvlJ3xtslIsg9ISrqiVpetZ1OkwIKKuO5s5RYWOIkEJ0G+CkACsyUApMuMqvhDOqWM36n7UppaMxZ7V5xAgnNj8TCclmSRmdlVgVqytkFuhS4DUJNKKLwXlNLWfHT7WTCA1ZadFfBiBJNlKOjKjrCie3QFEVY6XY+A7Pxqp2Qb5XShHtak2ZooiCeyMQTnneSIfFUEmHRFgxGxtG2Z6heJjKjZEdt/ON+v/8wOWTQYplytsVDBB6blXGAXS4LiuE4QjC0YJwjmtdwjVoSxGLs4xRHTql4XgGOeKTilXrQpMhtqSsnEY09ToB/Tu39DG2BF9OFx7/45K133tvtC/pM+TMvvZl98c3wI6KijqiBERZTcnuPaQILP/jVb334e//05OOfS6oL//Ot7/7l9/7ovz7ehaS28J0//42P/vR3fv3fn0MyufDt//6rv3/7w1/5tRvJlGwoDi8sUvON6RJlEFQ4Na+mUeHkvLKosDavYlRYJTYE6qX9aebH/aNmsOVFa+z4K8eO1o8cnp6arFUnxg/Zyli5ZEZH4mIh0koK7q1QnAKYB5oLTKUo/FQankuv7AdOEnAyBx7bD6wRsJYDj+4HThNwOp0FmAMo1Q+kmQGPHPjlawA/DVA6fCAwuGlpeghYmtq/skIrK+nBwDECjuXAyf3AMgHLObC2H0iVJk0GA+vVA2lmwIkDv8zkGj8QmMl1aFguu39lkVYW04OBBQIWcmBlPzAiYJQDx/YD45Qb0oz18oE0M2DpwC8zucyBwEyu0WG5Rvgh4KTOkFaqnEa8H6hTTmcZjeKBwIxGYQhIGymgCJ8heEkrZb5SDwFpjgt5+uNtJDdJ4NbsyZK/UKcUJFW6xEmNdqQG9duRP36lTOgiFnThg/f/7S9Kj7C6s3DiGU7uuGqOdOHP+j/4x+IjnCTITjKZ4174zh+//wfxI6zxF1M7rsYeT5Df/uCD/5WPcCp8MYWS0lSWnk9Rajmlpt0U0cqo1nYWfvJZhvCpm6TnDHuVIdmyqaf0FXESXgx9hdWnSRVrRDN7MTn8Ze1pUmOZ27SnprxdV65wuzSbnlIDIWs7C8efuVomgZskVjEHVhlYXfil7//tN777y7/7H5OPdhYm9hjIPnrqqlhb+PbXvvrN93/4L8/jsCZjY/JpUmOLJWQoKjhiPk4Y4uBlIsTUyZfJJFWevMWpgxbPPLxeoZ7hgnxJmj1mCP34nlYGuGqMSxOuCKXxXN3AefE5bP3EgWyR/7KAYXYIZ+FzOCocxBHVaDWsDkx9/hlWd1i3geTOQv0Z1sjtqhn2wZp59bO0bw2wZUvn1VdCeQ80j7aaOq6YuyuRt+vKfxNSby2dvJBu7VEEXwoXnk8pqiJ+HpX/B57uW/VpSD/hMyFhKdb8c+BKINQWseemjifWWSNAD2AxH3iZH9KsIbClZ2jeGHPZF848aPYtbnlxyfepMkHRQlhpZ3NwzdMGqiDD/Fc3/J/0d+E0wJHQV6D0Iyu0aVNr4amFl94u151sYTi+eLHTcFFKwmZ1qbInE6IjadIjMFSHELoXgSr1NO9r25OJCJ1+gfAQUR4NtIhUGg6KZCtRWAgnLwKVneAjlfAbpiNUJ0IYPWgM8+Ns4DEYNofDI+rjMfI2XeYJRGRdEq6MDaOWGyAOZ0NctAsut7kgpClIMKWoGDDLQtDQggfAjlqCbJYp9s0y8YXpJU85woSp3+/zslkqLuf4AEMu0eFHOCzq9ynC/XF+KpBPbd9w0NrOhvXdln8jNT8jZI9HwjzpD6MungiFMQl1msDnaNmUl2bd/pPjy+QI5sssgT0Z9ET68dpTv67aDuoo0hWERfalL6XLHTIiGHNBiB4PhanYHxzNcWEefDlBuFIO06g4G/4K8+VsTRxOIAbtdpgFcH8+OIHLR0rmS4DZaJTO2zpe5hpM85Ep69ScJZYGB0q6MRgTSjv64tgktGQnyOJy0Ovtt0+YfJlp4BEmpzMaYeajQPNKsKf6DG6+QlodfGmLFRMGWqUw34sdD9uySUMYP+Tnj0MqoOlF0IIph/okHPeZcfCH+bCmxWdexy/XzSQcKEAVMofI0gxYMGaSbU4TOhoTiYincsZUgH3KAR9Bd4yBwcmGKYXZkkWwBWaGJ7B0DmWKDMqvLb76OM2uLWPeEBCO0C+s397cut9p4q2725ut5gZ2muv3O1u3tpu338TNDq5d7zY7eK156+4NpP+VcK+5AVCGyyP3Opsb99ebnS1RutfZXG9ubTU3Tl97UxYerN3u3N8qnHn13LlXz5QerG3dOZ0dxI+fefXcq69fwNnzaxc31ubXr1089X8Jngj1TCEAAA==", "base64");
const bytes = gunzipSync(zipped);

const wasmModule = new WebAssembly.Module(bytes);
const wasmInstance = new WebAssembly.Instance(wasmModule, {
	"__wbindgen_placeholder__": {
		__wbindgen_object_drop_ref: (arg0: number) => {
			takeObject(arg0);
		},

		__wbg_buffer_ccaed51a635d8a2d: (arg0: number) => {
			const ret = getObject(arg0).buffer;
			return addHeapObject(ret);
		},

		__wbg_newwithbyteoffsetandlength_7e3eb787208af730: (arg0: number, arg1: number, arg2: number) => {
			const ret = new Uint8Array(getObject(arg0), arg1 >>> 0, arg2 >>> 0);
			return addHeapObject(ret);
		},

		__wbg_new_fec2611eb9180f95: (arg0: number) => {
			const ret = new Uint8Array(getObject(arg0));
			return addHeapObject(ret);
		},

		__wbindgen_memory: () => {
			const ret = wasm.memory;
			return addHeapObject(ret);
		},
	},
});

// biome-ignore lint/suspicious/noExplicitAny: WASM go brrrr...
const wasm: any = wasmInstance.exports;
module.exports.__wasm = wasm;
