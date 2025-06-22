//
//  Camera.m
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/14/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import "Camera.h"
#import "MainGameLayer.h"
#import "Character.h"
#import "MainCharacterController.h"
#import "cocos2d.h"
#import "AiInput.h"


@implementation Camera
//
@synthesize cameraPosition;
//
-(id) initWithGameLayer:(MainGameLayer*)g
{
    //
    if((self = [self init]))
    {
        gameLayer = g;
        return self;
    }
    return nil;
}
//
-(void) updateCameraWithCharacter:(CCSprite*)character
{
    //
    if(character.opacity == 0)
        return;
    //
    float bcPosX = [CCDirector sharedDirector].winSize.width / 2.0f;
    float bcPosY = [CCDirector sharedDirector].winSize.height / 2.0f;
    //
    CGPoint characterPosition = ccp(character.position.x,character.position.y);
    //
    float centerX,centerY,centerZ;
    float eyeX, eyeY,eyeZ;
    //
    // get camera positions
    [gameLayer.camera centerX:&centerX centerY:&centerY centerZ:&centerZ];
    [gameLayer.camera eyeX:&eyeX eyeY:&eyeY eyeZ:&eyeZ];
    //
    CGPoint newPositions = lastPos;
    //
    if([[AiInput sharedAiInput] mainCharacterPosition].x >= [[CoreSettings sharedCoreSettings] upperLeft].x +
                                                            CHARACTER_CAMERA_STOP_X)
    {
        if([[AiInput sharedAiInput] mainCharacterPosition].x <= [[CoreSettings sharedCoreSettings] upperRight].x -
                                                                CHARACTER_CAMERA_STOP_X)
        {
            newPositions = ccpLerp(ccp((characterPosition.x-bcPosX),(frozenLastPoints.y)), ccp(centerX,centerY), 0.2f);
            frozenLastPoints = newPositions;
            cameraPosition = newPositions;
        }
        else {
            xFrozenFlags = ccp(0,-1);
            cameraPosition = ccp(newPositions.x,newPositions.y);
        }
    }
    else {
        xFrozenFlags = ccp(-1,0);
        cameraPosition = ccp(newPositions.x,newPositions.y);
    }
    //
    if([[AiInput sharedAiInput] mainCharacterPosition].y >= [[CoreSettings sharedCoreSettings] bottomRight].y +
                                                            CHARACTER_CAMERA_STOP_Y)
    {
        if([[AiInput sharedAiInput] mainCharacterPosition].y <= [[CoreSettings sharedCoreSettings] upperRight].y -
                                                            CHARACTER_CAMERA_STOP_Y)
        {
            newPositions = ccpLerp(ccp((frozenLastPoints.x),(characterPosition.y-bcPosY)), ccp(centerX,centerY), 0.2f);
            frozenLastPoints = newPositions;
            cameraPosition = newPositions;
        }
        else {
            yFrozenFlags = ccp(-1,0);
            cameraPosition = ccp(newPositions.x,newPositions.y);
        }
    }
    else {
        cameraPosition = ccp(newPositions.x,newPositions.y);
    }
    //
    //
    [gameLayer.camera setCenterX:newPositions.x centerY:newPositions.y centerZ:300];
    [gameLayer.camera setEyeX:newPositions.x eyeY:newPositions.y eyeZ:402];
    lastPos = newPositions;
    //
}
//
-(void) restart
{
    
}
//
@end
