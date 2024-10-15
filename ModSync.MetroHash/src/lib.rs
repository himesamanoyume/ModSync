#![no_main]

use js_sys::Uint8Array;
use metrohash::MetroHash128;
use std::hash::Hasher;
use std::mem;
use wasm_bindgen::prelude::*;

#[wasm_bindgen]
pub fn metrohash128(data: &[u8]) -> Uint8Array {
    let mut hasher = MetroHash128::new();
    hasher.write(data);
    let hash: [u8; 16] = unsafe { mem::transmute(hasher.finish128()) };
    
    hash.as_slice().into()
}
