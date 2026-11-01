//
//  InputManager.m
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/12/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import "InputManager.h"
#import "MainCharacterController.h"
#import "MainGameLayer.h"
#import "Camera.h"
//
//
#define INACTIVE_KEY_CODE -999
//
@interface InputManager (PrivateMethods)
-(void) moveWithMouse:(CGPoint) mousePositions;
-(void) moveWithKeyBoard;
-(CGPoint) updateCharacterDirectionsAndRotations;
-(void) joystickListener;
@end
//
@implementation InputManager
//
@synthesize characterMoveType;
@synthesize newMouseInputPositions;
@synthesize gameLayer;
@synthesize inputIsEnabled;
//
InputManager *_sharedInputManager;
//
+(InputManager*) sharedInputManager
{
    if(!_sharedInputManager)
        _sharedInputManager = [[InputManager alloc] init];
    return _sharedInputManager;
}
//
-(id) init
{
    if((self = [super init]))
    {
        return self;
    }
    return nil;
}
//
-(void) runListener
{
    //
    inputIsEnabled = YES;
    // init fictional char keys
    for(int i =0;i<4;i++)
        moveArrowKeyFlags[i] = 0;
    //
    characterMoveType = (CharacterMoveType)[[GameSettingsManager sharedGameSettingsManager] controlType];
    //
}
//
-(void) joystickListener
{
    joySticks = [[DDHidJoystick allJoysticks] retain];
    [joySticks makeObjectsPerformSelector:@selector(setDelegate:) withObject:self];
}
//
-(void) ddhidJoystick:(DDHidJoystick *)joystick stick:(unsigned int)stick otherAxis:(unsigned int)otherAxis valueChanged:(int)value
{
    
}
//
-(void) ddhidJoystick:(DDHidJoystick *)joystick stick:(unsigned int)stick povNumber:(unsigned int)povNumber valueChanged:(int)value
{
    
}
//
- (void)ddhidJoystick:(DDHidJoystick *)joystick buttonDown:(unsigned)buttonNumber
{
    
}
//
- (void)ddhidJoystick:(DDHidJoystick *)joystick buttonUp:(unsigned)buttonNumber
{
    
}
//
-(void) proceedKey:(KeyBind*)key down:(BOOL)keyDownFlag
{
    //
    if(inputIsEnabled == NO)
        return;
    //
        if(keyDownFlag == YES)  // key down
        {
            switch (key.keyCode)
            {
                case KEY_UP:
                    if(characterMoveType == kOnlyKeyboard)
                    {
                        shootArrowKeyFlags[0] = 1;
                        break;
                    }
                    else shootArrowKeyFlags[0] = INACTIVE_KEY_CODE;
                case KEY_W:
                    moveArrowKeyFlags[0]=1;
                    break;
                case KEY_DOWN:
                    if(characterMoveType == kOnlyKeyboard)
                    {
                        shootArrowKeyFlags[1] = 1;
                        break;
                    }
                    else shootArrowKeyFlags[1] = INACTIVE_KEY_CODE;
                case KEY_S:
                    moveArrowKeyFlags[1] = 1;
                    break;
                case KEY_LEFT:
                    if(characterMoveType == kOnlyKeyboard)
                    {
                        shootArrowKeyFlags[2] = 1;
                        break;
                    }
                    else shootArrowKeyFlags[2] = INACTIVE_KEY_CODE;
                case KEY_A:
                    moveArrowKeyFlags[2] = 1;
                    break;
                case KEY_RIGHT:
                    if(characterMoveType == kOnlyKeyboard)
                    {
                        shootArrowKeyFlags[3] = 1;
                        break;
                    }
                    else shootArrowKeyFlags[3] = INACTIVE_KEY_CODE;
                case KEY_D:
                    moveArrowKeyFlags[3] = 1;
                    break;
                case KEY_SPACE:
                    [[GameManager sharedGameManager] characterSpecialAbilitie];
                    return;
                case ESC_KEY:
                    if([[GameManager sharedGameManager] gameIsPaused] == NO &&
                       [[GameManager sharedGameManager] gameIsFinished] == NO)
                        [[GameManager sharedGameManager] pauseGame];
                    else [[GameManager sharedGameManager] resumeGame];
                    break;
                default:
                    break;
            }
            if(characterMoveType == kOnlyKeyboard)
            {
                if(shootArrowKeyFlags[0] == 1 ||
                   shootArrowKeyFlags[1] == 1 ||
                   shootArrowKeyFlags[2] == 1 ||
                   shootArrowKeyFlags[3] == 1)
                {
                    if([[GameSettingsManager sharedGameSettingsManager] isAutoFireEnabled] == NO)
                        [[MainCharacterController sharedCharacterController] startShoot];
                }
            }
            
        }
        //
        else 
        {
            switch (key.keyCode)
            {
                case KEY_UP:
                    if(characterMoveType == kOnlyKeyboard)
                    {
                        shootArrowKeyFlags[0] = 0;
                        break;
                    }
                    else shootArrowKeyFlags[0] = INACTIVE_KEY_CODE;
                case KEY_W:
                    moveArrowKeyFlags[0]=0;
                    break;
                case KEY_DOWN:
                    if(characterMoveType == kOnlyKeyboard)
                    {
                        shootArrowKeyFlags[1] = 0;
                        break;
                    }
                    else shootArrowKeyFlags[1] = INACTIVE_KEY_CODE;
                case KEY_S:
                    moveArrowKeyFlags[1] = 0;
                    break;
                case KEY_LEFT:
                    if(characterMoveType == kOnlyKeyboard)
                    {
                        shootArrowKeyFlags[2] = 0;
                        break;
                    }
                    else shootArrowKeyFlags[2] = INACTIVE_KEY_CODE;
                case KEY_A:
                    moveArrowKeyFlags[2] = 0;
                    break;
                case KEY_RIGHT:
                    if(characterMoveType == kOnlyKeyboard)
                    {
                        shootArrowKeyFlags[3] = 0;
                        break;
                    }
                    else shootArrowKeyFlags[3] = INACTIVE_KEY_CODE;
                case KEY_D:
                    moveArrowKeyFlags[3] = 0;
                    break;
            default:
                break;
            }
            //
            if(characterMoveType == kOnlyKeyboard)
            {
                if(shootArrowKeyFlags[0] == 0 &&
                   shootArrowKeyFlags[1] == 0 &&
                   shootArrowKeyFlags[2] == 0 &&
                   shootArrowKeyFlags[3] == 0)
                {
                    if([[GameSettingsManager sharedGameSettingsManager] isAutoFireEnabled] == NO)
                        [[MainCharacterController sharedCharacterController] endShoot];
                }
            }
            //
        }
        //
}
//
-(void) moveWithMouse:(CGPoint) mousePositions
{
    //
    if(inputIsEnabled == NO)
        return;
    //
    [[MainCharacterController sharedCharacterController] moveCharacterToMousePositions:mousePositions];
}
//
-(void) moveWithKeyBoard
{
    //
    if(inputIsEnabled == NO)
        return;
    //
    [[MainCharacterController sharedCharacterController] moveCharacterWithKeys:moveArrowKeyFlags :shootArrowKeyFlags];
}
//
-(CGPoint) updateCharacterDirectionsAndRotations
{
    float cameraX,cameraY;//cameraZ;
    cameraX = [[[GameManager sharedGameManager] mainCamera] cameraPosition].x;
    cameraY = [[[GameManager sharedGameManager] mainCamera] cameraPosition].y;
    //[gameLayer.camera centerX:&cameraX centerY:&cameraY centerZ:&cameraZ];
    CGPoint newPos = ccp(newMouseInputPositions.x + cameraX,newMouseInputPositions.y + cameraY);
    [[MainCharacterController sharedCharacterController] rotateCharacterWithMousePos:newPos];
    return newPos;
}
//
-(void) inputEventListener
{
    //
    if(inputIsEnabled == NO)
        return;
    //
    BOOL characterIsDead = [[GameManager sharedGameManager] characterIsDead];
    //
    if([[GameManager sharedGameManager] gameIsPreparing] == YES)
        return;
    //
    CGPoint newPos = [self updateCharacterDirectionsAndRotations];
    //
    switch (characterMoveType) {
        case kKeyBoardAndMouse:
        case kOnlyKeyboard:
            [self moveWithKeyBoard];
            break;
        case kOnlyMouse:
            [self moveWithMouse:newPos];
            break;
        default:
            break;
    }
    
}
//
-(void) rotateWithMouse:(CGPoint)mousePosition
{
    //
    if(inputIsEnabled == NO)
        return;
    //
    newMouseInputPositions = mousePosition;
    //
}
//
-(void) restartInputManager
{
    //
    shootArrowKeyFlags[0] = 0;
    shootArrowKeyFlags[1] = 0;
    shootArrowKeyFlags[2] = 0;
    shootArrowKeyFlags[3] = 0;
    //
    moveArrowKeyFlags[0] = 0;
    moveArrowKeyFlags[1] = 0;
    moveArrowKeyFlags[2] = 0;
    moveArrowKeyFlags[3] = 0;
    //
    [[MainCharacterController sharedCharacterController] moveCharacterWithKeys:moveArrowKeyFlags :shootArrowKeyFlags];
    //
}
//
-(void) destroyInputManager
{
}
//
@end
