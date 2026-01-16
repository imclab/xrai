// H3MNativeLog.mm - Native iOS logging for H3M debug
// This allows logs to appear in Console.app and idevicesyslog

#import <Foundation/Foundation.h>

extern "C" {
    void _NSLog(const char* message) {
        NSLog(@"[H3M] %s", message);
    }
}
