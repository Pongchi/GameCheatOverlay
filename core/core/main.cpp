#include <iostream>
#include <windows.h>

using namespace std;

int main() {
	DWORD pid;
	LPCTSTR GameName = "Minecraft 1.20.6 - SIngleplayer";
	HWND hwnd = FindWindow(0, GameName);
	GetWindowThreadProcessId(hwnd, &pid);

	HANDLE handle = OpenProcess(PROCESS_ALL_ACCESS, 0, pid);

	if (!handle) cout << "Process is not opened!";
	else cout << "Process is opened!";

	CloseHandle(handle);
}