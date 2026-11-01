
//
//  HUDLayer.mm
//  GeorgianTale
//
//  Created by Giorgi  Abelashvili on 1/15/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "HUDLayer.h"
#import "MainCharacterController.h"
#import "KeyBind.h"
#import "InputManager.h"
#import "AiInput.h"

#define PAUSE_BUTTON_RECT           CGRectMake(460.0,300.0,30.0,30.0)
#define PAUSE_BUTTON_RECT_IPAD      CGRectMake(990.0f,730.0f,60.0f,60.0f)

@implementation HUDLayer

@synthesize mainGameLayerDelegate;

- (id) init
{
    //
	if(self == [super init])
	{
        //
		self.isKeyboardEnabled = YES;
		self.isMouseEnabled = YES;
        keyCodeCounter = 0;
		//
		return self;
	}
	//
	return nil;
}
//
-(BOOL) ccMouseMoved:(NSEvent *)event
{
    if([InputManager sharedInputManager].characterMoveType==kOnlyMouse || 
       [InputManager sharedInputManager].characterMoveType == kKeyBoardAndMouse)
    {
        //
        CGPoint mousePoints = [[CCDirector sharedDirector] convertEventToGL:event];
        [[GameManager sharedGameManager] rotateWithMouse:mousePoints];
        //
        [[AiInput sharedAiInput] setCursorPosition:mousePoints];
        //
    }
    return YES;
}
//
-(BOOL) ccMouseDragged:(NSEvent *)event
{
    //
    if([InputManager sharedInputManager].characterMoveType==kOnlyMouse || 
       [InputManager sharedInputManager].characterMoveType == kKeyBoardAndMouse)
    {
        //
        CGPoint mousePoints = [[CCDirector sharedDirector] convertEventToGL:event];
        if([[GameManager sharedGameManager] gameIsPaused] == NO)
            [[GameManager sharedGameManager] rotateWithMouse:mousePoints];
        //
    }
    //
    return YES;
}
//
-(BOOL) ccMouseDown:(NSEvent *)event
{
    //
    if([InputManager sharedInputManager].characterMoveType==kOnlyMouse ||
       [InputManager sharedInputManager].characterMoveType == kKeyBoardAndMouse)
    {
        //
        if([[GameSettingsManager sharedGameSettingsManager] isAutoFireEnabled] == NO)
        {
            if([[GameManager sharedGameManager] gameIsPaused] == NO && [[GameManager sharedGameManager] gameIsPreparing] == NO)
                [[MainCharacterController sharedCharacterController] startShoot];
        }
        //
    }
    //
    return YES;
}
//
-(BOOL) ccMouseUp:(NSEvent *)event
{
    //
    if([InputManager sharedInputManager].characterMoveType==kOnlyMouse ||
       [InputManager sharedInputManager].characterMoveType == kKeyBoardAndMouse)
    {
        if([[GameSettingsManager sharedGameSettingsManager] isAutoFireEnabled] == NO)
        {
            if([[GameManager sharedGameManager] gameIsPaused] == NO)
                [[MainCharacterController sharedCharacterController] endShoot];
        }
    }
    return YES;
}
//
-(BOOL) ccKeyDown:(NSEvent *)event
{
    //
    //if([InputManager sharedInputManager].characterMoveType == kKeyBoardAndMouse ||
    //   [InputManager sharedInputManager].characterMoveType == kOnlyKeyboard)
    //{
        //
        BOOL commandKeyDown = (([[NSApp currentEvent] modifierFlags] & NSCommandKeyMask) == NSCommandKeyMask);
        if(commandKeyDown == YES)
            return YES;
        //
        NSString *key = [event characters];
        unichar keyCode = [key characterAtIndex: 0];
        //
        int keyCodeInt = (int)keyCode;
        KeyBind *keyBind = [[KeyBind alloc] init];
        keyBind.keyCode = keyCodeInt;
        keyBind.keyQueue = keyCodeCounter;
        //
        // key down
        [[InputManager sharedInputManager] proceedKey:keyBind down:YES];
        //
    //}
    return YES;
}
//

//
-(BOOL) ccKeyUp:(NSEvent *)event
{
    //
    //if([InputManager sharedInputManager].characterMoveType == kKeyBoardAndMouse ||
    //   [InputManager sharedInputManager].characterMoveType == kOnlyKeyboard)
    //{
        //
        NSString *key = [event characters];
        unichar keyCode = [key characterAtIndex: 0];
        //
        int keyCodeInt = (int)keyCode;
        KeyBind *keyBind = [[KeyBind alloc] init];
        keyBind.keyCode = keyCodeInt;
        keyBind.keyQueue = keyCodeCounter;
        //
        [[InputManager sharedInputManager] proceedKey:keyBind down:NO];
        //
    //}
    return YES;
}
//
-(void) dealloc
{
    [super dealloc];
}
//
@end

