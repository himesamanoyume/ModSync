# ModSync.MetroHash

This package contains a WASM wrapper for [metrohash-rs](https://github.com/arthurprs/metrohash-rs). It is used by the [ModSync](https://github.com/ModSync/ModSync) mod management tool.

## Requirements

- Rust nightly toolchain
- wasm-pack (https://rustwasm.github.io/wasm-pack/)

## Build

```bash
cargo install
wasm-pack build --release --target nodejs --out-name metrohash --no-pack --manifest-path Cargo.toml -Z build-std=panic_abort,std -Z build-std-features=panic_immediate_abort
```