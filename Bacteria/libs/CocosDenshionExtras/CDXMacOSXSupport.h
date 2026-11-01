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

/**
 A set of proxy classes to allow iOS audio code to run on MacOS X. AVAudioPlayer is implemented using NSSound.
 AVAudioSession is a "do nothing" class as it isn't really relevant on MacOS X.
 
 Limitations:
 AVAudioPlayer numberOfLoops not correctly supported.  Looping is either on or off, can not specify a specific number of loops.
 AVAudioPlayer panning not supported.
 AVAudioPlayer metering not supported.
 AVAudioSession nothing is supported, not applicable to MacOS X.
 */

#import <Availability.h>
#ifdef __IPHONE_OS_VERSION_MAX_ALLOWED
#elif defined(__MAC_OS_X_VERSION_MAX_ALLOWED)

#import <Foundation/Foundation.h>
#import <AppKit/NSSound.h>
#import <AudioToolbox/AudioToolbox.h>
#import <AVFoundation/AVFoundation.h>

/*
 Modern macOS SDKs ship their own AVAudioPlayer (real one is used here) and may declare
 AudioSession / AVAudioSession symbols too. The iOS-compat stubs below are renamed so they
 can never collide with the SDK. These defines must stay AFTER the SDK imports above.
 */
#define kAudioSessionProperty_OtherAudioIsPlaying CDX_kAudioSessionProperty_OtherAudioIsPlaying
#define kAudioSessionProperty_AudioRoute          CDX_kAudioSessionProperty_AudioRoute
#define AudioSessionGetProperty                   CDX_AudioSessionGetProperty
#define AudioSessionProperties                    CDX_AudioSessionProperties
#define AVAudioSession                            CDXAudioSession
#define AVAudioSessionDelegate                    CDXAudioSessionDelegate
#define AVAudioSessionCategoryAmbient             CDXAudioSessionCategoryAmbient
#define AVAudioSessionCategorySoloAmbient         CDXAudioSessionCategorySoloAmbient
#define AVAudioSessionCategoryPlayback            CDXAudioSessionCategoryPlayback
#define AVAudioSessionCategoryRecord              CDXAudioSessionCategoryRecord
#define AVAudioSessionCategoryPlayAndRecord       CDXAudioSessionCategoryPlayAndRecord
#define AVAudioSessionCategoryAudioProcessing     CDXAudioSessionCategoryAudioProcessing
#define AVAudioSessionInterruptionFlags_ShouldResume CDXAudioSessionInterruptionFlags_ShouldResume
#define AVAudioSessionSetActiveFlags_NotifyOthersOnDeactivation CDXAudioSessionSetActiveFlags_NotifyOthersOnDeactivation

enum AudioSessionProperties {
	kAudioSessionProperty_OtherAudioIsPlaying,
	kAudioSessionProperty_AudioRoute
};

extern OSStatus AudioSessionGetProperty(UInt32 inID, UInt32 *ioDataSize, void *outData);

/**
 Taken from AVAudioSession.h header in AVFoundation headers
 */

/* This protocol is available with iPhone 3.0 or later */
@protocol AVAudioSessionDelegate;
@class NSError, NSString;

/* values for the category property */
extern NSString *const AVAudioSessionCategoryAmbient;
extern NSString *const AVAudioSessionCategorySoloAmbient;
extern NSString *const AVAudioSessionCategoryPlayback;
extern NSString *const AVAudioSessionCategoryRecord;
extern NSString *const AVAudioSessionCategoryPlayAndRecord;
extern NSString *const AVAudioSessionCategoryAudioProcessing;

enum {
	AVAudioSessionInterruptionFlags_ShouldResume = 1
};

enum {	
	AVAudioSessionSetActiveFlags_NotifyOthersOnDeactivation = 1
};

@interface AVAudioSession : NSObject {
	
	// properties
	NSString* category;
	double preferredHardwareSampleRate;
	NSTimeInterval preferredIOBufferDuration;
	
	BOOL inputIsAvailable;
	double currentHardwareSampleRate;
	NSInteger currentHardwareInputNumberOfChannels;
	NSInteger currentHardwareOutputNumberOfChannels;
	id<AVAudioSessionDelegate> delegate;

@private
    __strong void *_impl;
}

/* returns singleton instance */
+ (id)sharedInstance;

@property(assign) id<AVAudioSessionDelegate> delegate;

- (BOOL)setActive:(BOOL)beActive error:(NSError**)outError;
- (BOOL)setActive:(BOOL)beActive withFlags:(NSInteger)flags error:(NSError**)outError;

- (BOOL)setCategory:(NSString*)theCategory error:(NSError**)outError;
- (BOOL)setPreferredHardwareSampleRate:(double)sampleRate error:(NSError**)outError;
- (BOOL)setPreferredIOBufferDuration:(NSTimeInterval)duration error:(NSError**)outError;

@property(readonly) NSString* category;
@property(readonly) double preferredHardwareSampleRate;
@property(readonly) NSTimeInterval preferredIOBufferDuration;

@property(readonly) BOOL inputIsAvailable;
@property(readonly) double currentHardwareSampleRate;
@property(readonly) NSInteger currentHardwareInputNumberOfChannels;
@property(readonly) NSInteger currentHardwareOutputNumberOfChannels;

@end


/* A protocol for delegates of AVAudioSession */
@protocol AVAudioSessionDelegate <NSObject>
@optional 

- (void)beginInterruption;

- (void)endInterruptionWithFlags:(NSUInteger)flags;

- (void)endInterruption; /* endInterruptionWithFlags: will be called instead if implemented. */

- (void)inputIsAvailableChanged:(BOOL)isInputAvailable;
@end

#endif
