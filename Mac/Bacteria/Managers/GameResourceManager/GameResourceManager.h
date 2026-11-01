//
//  GameResourceManager.h
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/11/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "cocos2d.h"

@class CCSpriteBatchNodeBlur;
@interface GameResourceManager : NSObject
{
    ccBlendFunc originalBlendFunc;
}

+(GameResourceManager*) sharedGameResourceManager;
-(CCSpriteBatchNodeBlur*) sharedMainCharacterSpriteSheet;
-(CCSpriteBatchNodeBlur*) sharedVitaminsSpriteSheet;
-(CCSpriteBatchNodeBlur*) sharedEnemyArrowDirectioSpriteSheet;
-(void) updateWithSlowMotion:(BOOL) updateflag;
@end
