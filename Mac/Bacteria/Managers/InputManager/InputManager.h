//
//  InputManager.h
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/12/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#include <vector>
#import <DDHidLib/DDHidLib.h>
using namespace std;

@class KeyBind;
@class MainGameLayer;
@interface InputManager : NSObject
{
    @private
    CFMachPortRef mMachPortRef;
    CFRunLoopSourceRef mKeyboardEventSrc;
    NSTimer *keyBoardListenerTimerValue;
    int moveArrowKeyFlags[4];
    int shootArrowKeyFlags[4];
    CharacterMoveType characterMoveType;
    CGPoint newMouseInputPositions;
    MainGameLayer *gameLayer;
    NSArray *joySticks;
    BOOL inputIsEnabled;
}
//
@property (nonatomic,assign) CharacterMoveType characterMoveType;
@property (nonatomic,assign) CGPoint newMouseInputPositions;
@property (nonatomic,assign) MainGameLayer *gameLayer; 
@property (nonatomic,assign) BOOL inputIsEnabled;
//
+(InputManager*) sharedInputManager;
-(void) proceedKey:(KeyBind*)key down:(BOOL) keyDownFlag;
-(void) rotateWithMouse:(CGPoint)mousePosition;
-(void) destroyInputManager;
-(void) runListener;
-(void) inputEventListener;
-(void) restartInputManager;
//
@end
