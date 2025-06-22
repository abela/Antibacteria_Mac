//
//  SettingsLayer.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "SettingsLayer.h"
#import "cocos2d.h"
#import "MainMenuLayer.h"
//
NSString *difficultyLevelWords[3] = {[[LocalizationManager sharedLocalizationManager]
                                      getValueWithKey:_NORMAL],
                                    [[LocalizationManager sharedLocalizationManager]
                                     getValueWithKey:_HARD],
                                    [[LocalizationManager sharedLocalizationManager]
                                     getValueWithKey:_IMPOSSIBLE]};
NSString *controlTypeWords[3] = {[[LocalizationManager sharedLocalizationManager]
                                  getValueWithKey:_WASD_ARROWS],
                                [[LocalizationManager sharedLocalizationManager]
                                 getValueWithKey:_ARROWS],
                                [[LocalizationManager sharedLocalizationManager]
                                 getValueWithKey:_ONLY_MOUSE]};
//
@interface  SettingsLayer (Private)
-(void) initSettings;
-(void) changeLanguageToThisPage;
@end
//
@implementation SettingsLayer
+(CCScene*)scene
{
	CCScene *scene = [CCScene node];
	SettingsLayer *layer = [SettingsLayer node];
	[scene addChild: layer];
	return scene;
}
//
-(id) init
{
    if((self = [super init]))
    {
        [self initSettings];
        [self createLayerMenu];
        self.isKeyboardEnabled = YES;
        self.isMouseEnabled = YES;
        return self;
    }
    return nil;
}
//
-(void) initSettings
{
    gameDifficulty = (int)[[GameSettingsManager sharedGameSettingsManager] gameDifficulty];
    controlType = (int)[[GameSettingsManager sharedGameSettingsManager] controlType];
    autoFireIsOnValue = [[GameSettingsManager sharedGameSettingsManager] isAutoFireEnabled];
}
//
-(void) createLayerMenu
{
    float aspectRatio = [[CoreSettings sharedCoreSettings] aspectRatio];
    // settings title
    titleLabel = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_GAME_OPTIONS]
                                              dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                              hAlignment:kCCTextAlignmentCenter
                                                fontName:@"Times New Roman"
                                                fontSize:50*aspectRatio];
    float x = [CCDirector sharedDirector].winSize.width / 2.0f;
    float y = [CCDirector sharedDirector].winSize.height  - 100.0f*aspectRatio;
    //
    titleLabel.position = ccp(x,y);
    titleLabel.color = ccc3(0, 255, 0);
    [self addChild:titleLabel];
    //
    //
    //
    //
    //
    languageTitleLabel = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_LANGUAGE]
                                       dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                       hAlignment:kCCTextAlignmentCenter
                                         fontName:@"Times New Roman"
                                         fontSize:30*aspectRatio];
    //
    x = [CCDirector sharedDirector].winSize.width / 4.0f;
    y = [CCDirector sharedDirector].winSize.height - 200*aspectRatio;
    //
    languageTitleLabel.position = ccp(x,y);
    languageTitleLabel.color = ccc3(0, 255, 0);
    [self addChild:languageTitleLabel];
    //
    //
    //
    languageLabel = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_LANGUAGE_VALUE]
                                            dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                            hAlignment:kCCTextAlignmentCenter
                                              fontName:@"Times New Roman"
                                              fontSize:30*aspectRatio];
    //
    x = [CCDirector sharedDirector].winSize.width / 2.0f + [CCDirector sharedDirector].winSize.width / 4.0f;
    y = [CCDirector sharedDirector].winSize.height - 200*aspectRatio;
    //
    languageLabel.position = ccp(x,y);
    languageLabel.color = ccc3(0, 255, 0);
    [self addChild:languageLabel];
    //
    //
    // difficulty left arrow
    languageLeft = [CCSprite spriteWithFile:@"arrow.png"];
    languageLeft.position = ccp(x - 150.0f*aspectRatio,y + 20.0f*aspectRatio);
    languageLeft.scale = 0.5f*aspectRatio;
    [self addChild:languageLeft];
    //
    //
    // difficulty right arrow
    languageRight= [CCSprite spriteWithFile:@"arrow.png"];
    languageRight.position = ccp(x + 150.0f*aspectRatio,y + 20.0f*aspectRatio);
    languageRight.scale = 0.5f*aspectRatio;
    languageRight.flipX = YES;
    [self addChild:languageRight];
    //
    //
    //
    //
    difficultyLabel = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_DIFFICULTY]
                                              dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                              hAlignment:kCCTextAlignmentCenter
                                                fontName:@"Times New Roman"
                                                fontSize:30*aspectRatio];
    //
    x = [CCDirector sharedDirector].winSize.width / 4.0f;
    y = [CCDirector sharedDirector].winSize.height - 300*aspectRatio;
    //
    difficultyLabel.position = ccp(x,y);
    difficultyLabel.color = ccc3(0, 255, 0);
    [self addChild:difficultyLabel];
    //
    //
    //
    difficultyValueLabel = [CCLabelTTF labelWithString:difficultyLevelWords[gameDifficulty]
                                            dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                            hAlignment:kCCTextAlignmentCenter
                                              fontName:@"Times New Roman"
                                              fontSize:30*aspectRatio];
    //
    x = [CCDirector sharedDirector].winSize.width / 2.0f + [CCDirector sharedDirector].winSize.width / 4.0f;
    y = [CCDirector sharedDirector].winSize.height - 300*aspectRatio;
    //
    difficultyValueLabel.position = ccp(x,y);
    difficultyValueLabel.color = ccc3(0, 255, 0);
    [self addChild:difficultyValueLabel];
    //
    //
    // difficulty left arrow
    difficultyLeft = [CCSprite spriteWithFile:@"arrow.png"];
    difficultyLeft.position = ccp(x - 150.0f*aspectRatio,y + 20.0f*aspectRatio);
    difficultyLeft.scale = 0.5f*aspectRatio;
    [self addChild:difficultyLeft];
    //
    //
    // difficulty right arrow
    difficultyRight = [CCSprite spriteWithFile:@"arrow.png"];
    difficultyRight.position = ccp(x + 150.0f*aspectRatio,y + 20.0f*aspectRatio);
    difficultyRight.scale = 0.5f*aspectRatio;
    difficultyRight.flipX = YES;
    [self addChild:difficultyRight];
    //
    //
    //
    autoShootLabel = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager]
                                                              getValueWithKey:_AUTO_SHOOT]
                                                  dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                                  hAlignment:kCCTextAlignmentCenter
                                                    fontName:@"Times New Roman"
                                                    fontSize:30*aspectRatio];
    //
    x = [CCDirector sharedDirector].winSize.width / 4.0f;
    y = [CCDirector sharedDirector].winSize.height - 400*aspectRatio;
    //
    autoShootLabel.position = ccp(x,y);
    autoShootLabel.color = ccc3(0, 255, 0);
    [self addChild:autoShootLabel];
    //
    //
    autoFireIsOn = [CCSprite spriteWithFile:@"Play_Normal.png"];
    autoFireIsOn.position = ccp([CCDirector sharedDirector].winSize.width /2.0f +
                                [CCDirector sharedDirector].winSize.width / 4.0f,y);
    autoFireIsOn.scale = 0.5f*aspectRatio;
    autoFireIsOn.opacity = ((autoFireIsOnValue == YES) ? 255 : 128);
    [self addChild:autoFireIsOn];
    //
    //
    controlTypeLabel = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager]
                                                                getValueWithKey:_CONTROL_TYPE]
                                                  dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                                  hAlignment:kCCTextAlignmentCenter
                                                    fontName:@"Times New Roman"
                                                    fontSize:30*aspectRatio];
    //
    x = [CCDirector sharedDirector].winSize.width / 4.0f;
    y = [CCDirector sharedDirector].winSize.height - 500*aspectRatio;
    //
    controlTypeLabel.position = ccp(x,y);
    controlTypeLabel.color = ccc3(0, 255, 0);
    [self addChild:controlTypeLabel];
    //
    //
    //
    controlLabel = [CCLabelTTF labelWithString:controlTypeWords[controlType]
                                            dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                            hAlignment:kCCTextAlignmentCenter
                                              fontName:@"Times New Roman"
                                              fontSize:30*aspectRatio];
    //
    x = [CCDirector sharedDirector].winSize.width / 2.0f + [CCDirector sharedDirector].winSize.width / 4.0f;
    y = [CCDirector sharedDirector].winSize.height  - 500*aspectRatio;
    //
    controlLabel.position = ccp(x,y);
    controlLabel.color = ccc3(0, 255, 0);
    [self addChild:controlLabel];
    //
    //
    //
    // controls left arrow
    controlTypeLeft = [CCSprite spriteWithFile:@"arrow.png"];
    controlTypeLeft.position = ccp(x - 150.0f*aspectRatio,y + 20.0f*aspectRatio);
    controlTypeLeft.scale = 0.5f*aspectRatio;
    [self addChild:controlTypeLeft];
    //
    //
    // controls right arrow
    controlTypeRight = [CCSprite spriteWithFile:@"arrow.png"];
    controlTypeRight.position = ccp(x + 150.0f*aspectRatio,y + 20.0f*aspectRatio);
    controlTypeRight.scale = 0.5f*aspectRatio;
    controlTypeRight.flipX = YES;
    [self addChild:controlTypeRight];
    //
    //
    // sfx enabled label
    sfxEnabledLabel = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager]
                                                               getValueWithKey:_SOUND_SFX]
                                                    dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                                    hAlignment:kCCTextAlignmentCenter
                                                      fontName:@"Times New Roman"
                                                      fontSize:30*aspectRatio];
    //
    x = [CCDirector sharedDirector].winSize.width / 4.0f;
    y = [CCDirector sharedDirector].winSize.height- 600*aspectRatio;
    //
    sfxEnabledLabel.position = ccp(x,y);
    sfxEnabledLabel.color = ccc3(0, 255, 0);
    [self addChild:sfxEnabledLabel];
    //
    //
    //
    // controls right arrow
    sfxIsEnabledSprite = [CCSprite spriteWithFile:@"Play_Normal.png"];
    sfxIsEnabledSprite.position = ccp([CCDirector sharedDirector].winSize.width / 2.0f +
                                [CCDirector sharedDirector].winSize.width / 4.0f,y + 20.0f*aspectRatio);
    sfxIsEnabledSprite.scale = 0.5f*aspectRatio;
    sfxIsEnabledSprite.opacity = ([[GameSettingsManager sharedGameSettingsManager] sfxIsEnabled] ? 255 : 128);
    [self addChild:sfxIsEnabledSprite];
    //
    //
    // sfx enabled label
    soundIsEnabled = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager]
                                                              getValueWithKey:_GAME_MUSIC]
                                                   dimensions:CGSizeMake(500*aspectRatio,60*aspectRatio)
                                                   hAlignment:kCCTextAlignmentCenter
                                                     fontName:@"Times New Roman"
                                                     fontSize:30*aspectRatio];
    //
    x = [CCDirector sharedDirector].winSize.width / 4.0f;
    y = [CCDirector sharedDirector].winSize.height - 700*aspectRatio;
    //
    soundIsEnabled.position = ccp(x,y);
    soundIsEnabled.color = ccc3(0, 255, 0);
    [self addChild:soundIsEnabled];
    //
    //
    //
    soundIsEnabledSprite = [CCSprite spriteWithFile:@"Play_Normal.png"];
    soundIsEnabledSprite.position = ccp([CCDirector sharedDirector].winSize.width / 2.0f +
                                [CCDirector sharedDirector].winSize.width / 4.0f,y + 20.0f*aspectRatio);
    soundIsEnabledSprite.scale = 0.5f*aspectRatio;
    soundIsEnabledSprite.opacity = ([[GameSettingsManager sharedGameSettingsManager] soundIsEnabled] ? 255 : 128);
    [self addChild:soundIsEnabledSprite];

}
//
-(BOOL) ccMouseDown:(NSEvent *)event
{
    CGPoint mousePositions = [[CCDirector sharedDirector] convertEventToGL:event];
    //
    // autofire enabled button
    if (CGRectContainsPoint(autoFireIsOn.boundingBox,mousePositions)) {
        autoFireIsOnValue = !autoFireIsOnValue;
        [[GameSettingsManager sharedGameSettingsManager] setIsAutoFireEnabled:autoFireIsOnValue];
        [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
        autoFireIsOn.opacity = ((autoFireIsOnValue == YES) ? 255.0f : 128.0f);
    }
    //
    //difficulty left
    if (CGRectContainsPoint(difficultyLeft.boundingBox,mousePositions)) {
        gameDifficulty--;
        if(gameDifficulty<0)
            gameDifficulty = 2;
        difficultyValueLabel.string = difficultyLevelWords[gameDifficulty];
        //
        GameDifficulty difficulty = (GameDifficulty)gameDifficulty;
        [[GameSettingsManager sharedGameSettingsManager] setGameDifficulty:difficulty];
        [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
    }
    // difficulty right
    //
    if (CGRectContainsPoint(difficultyRight.boundingBox,mousePositions)) {
        gameDifficulty++;
        if(gameDifficulty>2)
            gameDifficulty = 0;
        difficultyValueLabel.string = difficultyLevelWords[gameDifficulty];
        //
        GameDifficulty difficulty = (GameDifficulty)gameDifficulty;
        [[GameSettingsManager sharedGameSettingsManager] setGameDifficulty:difficulty];
        [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
    }
    //
    
    //control left
    if (CGRectContainsPoint(controlTypeLeft.boundingBox,mousePositions)) {
        controlType--;
        if(controlType<0)
            controlType = 2;
        controlLabel.string = controlTypeWords[controlType];
        //
        CharacterMoveType characterMoveType = (CharacterMoveType)controlType;
        [[GameSettingsManager sharedGameSettingsManager] setControlType:characterMoveType];
        [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
    }
    // control right
    //
    if (CGRectContainsPoint(controlTypeRight.boundingBox,mousePositions)) {
        controlType++;
        if(controlType>2)
            controlType = 0;
        controlLabel.string = controlTypeWords[controlType];
        //
        CharacterMoveType characterMoveType = (CharacterMoveType)controlType;
        [[GameSettingsManager sharedGameSettingsManager] setControlType:characterMoveType];
        [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
    }
    //
    // sfx enabled
    if (CGRectContainsPoint(sfxIsEnabledSprite.boundingBox,mousePositions)) {
        BOOL sfxIsEnabled = ([[GameSettingsManager sharedGameSettingsManager] sfxIsEnabled]);
        sfxIsEnabled = !sfxIsEnabled;
        [[GameSettingsManager sharedGameSettingsManager] setSfxIsEnabled:sfxIsEnabled];
        [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
        sfxIsEnabledSprite.opacity = ((sfxIsEnabled == YES) ? 255 : 128);
    }
    //
    // sound enabled
    if (CGRectContainsPoint(soundIsEnabledSprite.boundingBox,mousePositions)) {
        BOOL soundIsEnabledVal = ([[GameSettingsManager sharedGameSettingsManager] soundIsEnabled]);
        soundIsEnabledVal = !soundIsEnabledVal;
        [[GameSettingsManager sharedGameSettingsManager] setSoundIsEnabled:soundIsEnabledVal];
        [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
        soundIsEnabledSprite.opacity = ((soundIsEnabledVal == YES) ? 255 : 128);
    }
    //
    // language left
    if (CGRectContainsPoint(languageLeft.boundingBox, mousePositions)) {
        //
        int currentLocalization = (int)[[LocalizationManager sharedLocalizationManager] currentLocalization];
        currentLocalization--;
        if (currentLocalization < 0) {
            currentLocalization = LANGUAGE_COUNT;
        }
        //
        [[LocalizationManager sharedLocalizationManager] setCurrentLocalization:((Localizations)currentLocalization)];
        [[LocalizationManager sharedLocalizationManager] saveCurrentLocalization];
        [self changeLanguageToThisPage];
        //
    }
    // language right
    if (CGRectContainsPoint(languageRight.boundingBox, mousePositions)) {
        //
        int currentLocalization = (int)[[LocalizationManager sharedLocalizationManager] currentLocalization];
        currentLocalization++;
        if (currentLocalization > LANGUAGE_COUNT) {
            currentLocalization = 0;
        }
        //
        [[LocalizationManager sharedLocalizationManager] setCurrentLocalization:((Localizations)currentLocalization)];
        [[LocalizationManager sharedLocalizationManager] saveCurrentLocalization];
        [self changeLanguageToThisPage];
        //
    }
    //
    return YES;
}

//
-(BOOL) ccKeyDown:(NSEvent *)event
{
    NSString *key = [event characters];
    unichar keyCode = [key characterAtIndex: 0];
    if(keyCode == ESC_KEY)
    {
        [[CCDirector sharedDirector] replaceScene:[MainMenuLayer scene]];
    }
    return YES;
}
//
-(void) changeLanguageToThisPage
{
    languageTitleLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_LANGUAGE];
    languageLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_LANGUAGE_VALUE];
    titleLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_GAME_OPTIONS];
    difficultyLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_DIFFICULTY];
    autoShootLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_AUTO_SHOOT];
    controlTypeLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_CONTROL_TYPE];
    sfxEnabledLabel.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_SOUND_SFX];
    soundIsEnabled.string = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_GAME_MUSIC];
    //
    //
    difficultyLevelWords[0] = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_NORMAL];
    difficultyLevelWords[1] = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_HARD];
    difficultyLevelWords[2] = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_IMPOSSIBLE];
    //
    controlTypeWords[0] = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_WASD_ARROWS];
    controlTypeWords[1] = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_ARROWS];
    controlTypeWords[2] = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_ONLY_MOUSE];
    //
    difficultyValueLabel.string = difficultyLevelWords[gameDifficulty];
    controlLabel.string = controlTypeWords[controlType];
}
//
-(void) onExit
{
    self.isKeyboardEnabled = NO;
    self.isMouseEnabled = NO;
}
//
@end
