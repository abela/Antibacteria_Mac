/*
 Copyright (c) 2010 Steve Oldmeadow
 
 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:
 
 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
 
 $Id$
 */

#import <Availability.h>
#ifdef __IPHONE_OS_VERSION_MAX_ALLOWED
#elif defined(__MAC_OS_X_VERSION_MAX_ALLOWED)

#import "CDXMacOSXSupport.h"
#import "SimpleAudioEngine.h"

NSString * const AVAudioSessionCategoryAmbient = @"AVAudioSessionCategoryAmbient";
NSString *const AVAudioSessionCategorySoloAmbient = @"AVAudioSessionCategorySoloAmbient";
NSString *const AVAudioSessionCategoryPlayback = @"AVAudioSessionCategoryPlayback";
NSString *const AVAudioSessionCategoryRecord = @"AVAudioSessionCategoryRecord";
NSString *const AVAudioSessionCategoryPlayAndRecord = @"AVAudioSessionCategoryPlayAndRecord";
NSString *const AVAudioSessionCategoryAudioProcessing = @"AVAudioSessionCategoryAudioProcessing";

OSStatus AudioSessionGetProperty(UInt32 inID, UInt32 *ioDataSize, void *outData) {
	//TODO: set outData appropriately
	return 0;
}    


/**
 A "do nothing" implementation - AVAudioSession is not really relevant to Mac OS X.
 */
@implementation AVAudioSession
@synthesize delegate, category, preferredHardwareSampleRate, preferredIOBufferDuration;
@synthesize inputIsAvailable, currentHardwareSampleRate, currentHardwareInputNumberOfChannels, currentHardwareOutputNumberOfChannels;

+ (id)sharedInstance {
	return nil;
}	

- (BOOL)setActive:(BOOL)beActive error:(NSError**)outError {return YES;}
- (BOOL)setActive:(BOOL)beActive withFlags:(NSInteger)flags error:(NSError**)outError {return YES;}
- (BOOL)setCategory:(NSString*)theCategory error:(NSError**)outError {return YES;}
- (BOOL)setPreferredHardwareSampleRate:(double)sampleRate error:(NSError**)outError {return YES;}
- (BOOL)setPreferredIOBufferDuration:(NSTimeInterval)duration error:(NSError**)outError {return YES;}

@end
#endif