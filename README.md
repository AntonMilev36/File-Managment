# File Management System

A **C# virtual file system** implemented inside a single binary container file.  
The system supports directories, file import/export, deletion, navigation, and **Huffman-compressed file storage** with checksum validation.

This project behaves like a lightweight shell, operating on a **custom filesystem format** rather than the host OS filesystem.

---

## 📦 Features

- Custom binary container (`fs_container.bin`)
- Hierarchical directory structure
- File compression using **Huffman encoding**
- Incremental checksum verification
- Multi-block file storage (linked blocks)
- Recursive directory deletion
- Interactive shell or command-line execution
- No external dependencies

---

## 🚀 Getting Started

```bash
git clone https://github.com/AntonMilev36/File-Managment.git
```

### Build & Run

```bash
dotnet build
dotnet run
```

---

## 📜 Supported Commands

### 📁 Directory Commands

| Command     | Description                        |
| ----------- | ---------------------------------- |
| `ls`        | List contents of current directory |
| `md <name>` | Create directory                   |
| `cd <name>` | Enter directory                    |
| `cd ..`     | Go to parent directory             |
| `cd /`      | Go to root                         |
| `rd <name>` | Remove directory recursively       |


### 📄 File Commands

| Command                                  | Description                      |
| ---------------------------------------- | -------------------------------- |
| `cpin <source_path> <target_name>`       | Copy file from OS into container |
| `cpout <source_name> <destination_path>` | Export file to OS                |
| `rm <file_name>`                         | Delete file from container       |

---

## 🧠 Internal Design

| Structure        | Size (Bytes) | Count |
|------------------|--------------|-------|
| File Count       | 4            | 1     |    
| Metadata Entries | 61           | 100   |
| Data Blocks      | 512          | 100   |


---

## 🧬 Compression

- Files are compressed using Huffman coding
- Frequency table is stored before compressed data
- Data spans multiple fixed-size blocks
- Blocks are linked via metadata
- On extraction, checksum validation ensures integrity

---

## 🧪 Error Handling

- Missing files
- Duplicate names
- Invalid commands
- Container full
- Checksum mismatches

---

## 📄 License
This project is **not licensed** for commercial use. 
Intended for **educational and demo purposes**.

## 🙋‍♂️ Author
Created by **Anton Milev** as part of an university project.
