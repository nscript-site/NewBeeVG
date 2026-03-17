
def foo():
    print("[foo] hello, pythonnet from python")
    return "foo result"

def add(a, b):
    return a + b

def process_bytes(data: bytes) -> bytes:
    # data 就是 C# 传过来的 bytes
    print(f"[python] 收到二进制长度: {len(data)}")
    
    # 处理...
    return b"response_data"  # 返回二进制