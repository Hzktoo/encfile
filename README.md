# encfile — Simple AES File Encryptor (C# .NET 9 CLI)

A minimal, fast, and secure command-line utility for AES-256 file encryption and decryption.  

---

## Features

- AES-256 encryption with CBC mode & PBKDF2 (SHA-256, 100k iterations)  
- Random 32-byte salt & 16-byte IV for each file  
- Simple usage — one command to encrypt or decrypt  
- Zero memory leaks: keys are wiped from memory  
- Self-contained binary (`encfile`) — no dependencies needed  

---

## Installation

Clone and install globally:

```bash
git clone https://github.com/<yourname>/encfile.git
cd encfile
chmod +x install.sh
./install.sh
````

This will:

* Build a **self-contained** Linux binary for `.NET 9`
* Copy it to `~/.local/bin/encfile`
* Make it globally available as a terminal command

After installation, ensure `~/.local/bin` is in your PATH (usually it is).

Check with:

```bash
which encfile
```

---

## Usage

### Encrypt a file

```bash
encfile testdocument.pdf
```

You’ll be asked for a password and its confirmation.

Output example:

```
Password: ********
Confirm password: ********
✓ Encrypted: testdocument.pdf.enc
  Original size: 142.12 KB
  Encrypted size: 162.43 KB
```

### Decrypt a file

```bash
encfile -d testdocument.pdf.enc
```

Output example:

```
Password: ********
✓ Decrypted: testdocument.pdf
  Size: 142.12 KB
```

---

## How it works

1. Derives a 256-bit key from your password using:

   ```
   PBKDF2(password, salt, 100000 iterations, SHA-256)
   ```
2. Encrypts data with:

   ```
   AES-256-CBC + PKCS7 padding
   ```
3. Prepends `salt || iv || ciphertext` into the `.enc` file.
4. During decryption, salt and IV are read and the same key is derived.

---

## Uninstall

```bash
rm ~/.local/bin/encfile
```




