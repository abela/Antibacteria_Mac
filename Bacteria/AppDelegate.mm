//
//  AppDelegate.m
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/10/13.
//  Copyright __MyCompanyName__ 2013. All rights reserved.
//

#import "AppDelegate.h"
#import "MainGameScene.h"
#import "GameResourceManager.h"
#import "MainMenuScene.h"
#import "NSUserDefaults+MPSecureUserDefaults.h"
#import "ScoreManager.h"
#import "MainMenuLayer.h"
#import "CoreSettings.h"
#import "GameCenterManager.h"
//
#define DEFAULT_SCREEN_WIDTH        1024
//
@implementation BacteriaAppDelegate
@synthesize window=window_, glView=glView_;

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
    //
    // init localization manager
    [LocalizationManager sharedLocalizationManager];
    //
    //[NSUserDefaults resetStandardUserDefaults];
    //
    [NSUserDefaults setSecret:@"ms3_ser_202$"];
    //
    CCDirectorMac *director = (CCDirectorMac*) [CCDirector sharedDirector];
    NSRect f = glView_.frame;
    NSRect f2 = [window_ frame];
    CGFloat titleBarHeight = f2.size.height - f.size.height; //i have titlebar and i whant calulate it height
    
    NSRect mainDisplayRect = [[NSScreen mainScreen] frame];
    CGFloat display_width = mainDisplayRect.size.width;
    CGFloat display_height = mainDisplayRect.size.height;
    //
    float gameScreenAspectRatio = (display_width > DEFAULT_SCREEN_WIDTH) ? (DEFAULT_SCREEN_WIDTH / display_width) : (display_width / DEFAULT_SCREEN_WIDTH);
    //
    [[CoreSettings sharedCoreSettings] setAspectRatio:gameScreenAspectRatio];
    //
    double ar = mainDisplayRect.size.width / mainDisplayRect.size.height;
    //
    CGFloat min_width = DEFAULT_SCREEN_WIDTH; //default window width on app start
    CGFloat min_height = roundf(min_width / ar); // calculated window height on app start
    
    CGFloat work_width = DEFAULT_SCREEN_WIDTH; //default working resolution witdh
    CGFloat work_height = roundf(work_width / ar); //calculated working resolution height
    
    [window_ setFrame:NSMakeRect(0, 0, min_width, min_height + titleBarHeight) display:YES];
    [glView_ setFrame:CGRectMake(f.origin.x, f.origin.y, work_width, work_height)];
    [director setView:glView_];
    [director setResizeMode:kCCDirectorResize_AutoScale];
    [glView_ setFrame:CGRectMake(0, 0, min_width, min_height)];
    [window_ setContentAspectRatio:NSMakeSize(display_width, display_height)];
    //
    NSScreen * screen = window_.screen;
    NSRect r = screen.frame;
    [window_ setFrameTopLeftPoint:NSMakePoint(r.size.width/2 - window_.frame.size.width/2, r.size.height/2 + window_.frame.size.height/2)];
    //
    // Enable "moving" mouse event. Default no.
    [window_ setAcceptsMouseMovedEvents:YES];
    //
    [GameResourceManager sharedGameResourceManager];
    //
    //
	[director runWithScene:[MainMenuLayer scene]];
    [CCTexture2D setDefaultAlphaPixelFormat:kCCTexture2DPixelFormat_RGBA8888];
    //[self toggleFullScreen:nil];
    //[[ScoreManager sharedScoreManager] authenticateLocalPlayer];
    //[[ScoreManager sharedScoreManager] showGameCenterOnWindow:window_];
    //
    // uncomment these lines in real use !!
    //[GameCenterManager sharedManager].window = window_;
    //[[GameCenterManager sharedManager] authenticateLocalPlayer];
    //
    /*if (![[NSFileManager defaultManager] fileExistsAtPath:[[[NSBundle mainBundle] appStoreReceiptURL] path]]) {
        NSLog(@"Must be launched from outside!");
        exit(173);
    }*/
    [[CoreSettings sharedCoreSettings] setMainWindow:window_];
}

- (BOOL) applicationShouldTerminateAfterLastWindowClosed: (NSApplication *) theApplication
{
	return YES;
}

- (void)dealloc
{
	[[CCDirector sharedDirector] end];
	[window_ release];
	[super dealloc];
}

#pragma mark AppDelegate - IBActions

- (IBAction)toggleFullScreen: (id)sender
{
    CCGLView *view= (CCGLView *)[[CCDirector sharedDirector] view];
	if (![view isInFullScreenMode]) {
		[view enterFullScreenMode:[NSScreen mainScreen] withOptions: nil];
	} else {
		[view exitFullScreenModeWithOptions:nil];
		[view.window makeFirstResponder: view];
	}
}

@end
