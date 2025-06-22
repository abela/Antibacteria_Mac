//
//  GameResourceManager.m
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/11/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import "GameResourceManager.h"
#import "CCSpriteBatchNodeBlur.h"
//
@interface GameResourceManager (PrivateMethods)
-(void) initGameResources;
@end 
//
@implementation GameResourceManager

//
GameResourceManager *_sharedGameResourceManager = nil;
CCSpriteBatchNodeBlur *_sharedMainCharacterSpriteSheet = nil;
CCSpriteBatchNodeBlur *_sharedVitaminsSpriteSheet = nil;
CCSpriteBatchNodeBlur *_sharedEnemyArrowDirectioSpriteSheet = nil;
//
+(GameResourceManager*) sharedGameResourceManager
{
    @synchronized([GameResourceManager class])
    {
        if(!_sharedGameResourceManager)
            _sharedGameResourceManager = [[GameResourceManager alloc] init];
        return _sharedGameResourceManager;
    }
}
//
-(void) initGameResources
{
    [[CCSpriteFrameCache sharedSpriteFrameCache] addSpriteFramesWithFile:@"characters_resources.plist"];
    [[CCSpriteFrameCache sharedSpriteFrameCache] addSpriteFramesWithFile:@"bonuses_spritesheet.plist"];
    [[CCSpriteFrameCache sharedSpriteFrameCache] addSpriteFramesWithFile:@"enemy_direction.plist"];
}
//
-(id) init
{
    if((self = [super init]))
    {
        // init game resources when we create instance
        [self initGameResources];
        return self;
    }
    return nil;
}
//
-(void) updateWithSlowMotion:(BOOL) updateflag
{
    //
    if(updateflag == YES)
    {
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet]
         setBlendFunc:(ccBlendFunc){ GL_DST_ALPHA , GL_DST_ALPHA}];
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] setBlendIsDisabled:NO];
        //
        [[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet]
         setBlendFunc:(ccBlendFunc){ GL_DST_ALPHA , GL_DST_ALPHA}];
        [[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] setBlendIsDisabled:NO];
        //
    }
    //
    else
    {
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] setBlendIsDisabled:YES];
        [[[GameResourceManager sharedGameResourceManager] sharedVitaminsSpriteSheet] setBlendIsDisabled:YES];
    }
    //
}
//
-(CCSpriteBatchNodeBlur*) sharedVitaminsSpriteSheet
{
    //
    if(!_sharedVitaminsSpriteSheet)
    {
        originalBlendFunc = _sharedVitaminsSpriteSheet.blendFunc;
        _sharedVitaminsSpriteSheet = [CCSpriteBatchNodeBlur batchNodeWithFile:@"bonuses_spritesheet.png"];
    }
    return _sharedVitaminsSpriteSheet;
    //
}
//
-(CCSpriteBatchNodeBlur*) sharedMainCharacterSpriteSheet
{
    //
    if(!_sharedMainCharacterSpriteSheet)
    {
        originalBlendFunc = _sharedMainCharacterSpriteSheet.blendFunc;
        _sharedMainCharacterSpriteSheet = [CCSpriteBatchNodeBlur batchNodeWithFile:@"characters_resources.png"];
    }
    return _sharedMainCharacterSpriteSheet;
    //
}
//
-(CCSpriteBatchNodeBlur*) sharedEnemyArrowDirectioSpriteSheet
{
    if(!_sharedEnemyArrowDirectioSpriteSheet)
    {
        originalBlendFunc = _sharedMainCharacterSpriteSheet.blendFunc;
        _sharedEnemyArrowDirectioSpriteSheet = [CCSpriteBatchNodeBlur batchNodeWithFile:@"enemy_direction.png"];
    }
    return _sharedEnemyArrowDirectioSpriteSheet;
}
//
@end
