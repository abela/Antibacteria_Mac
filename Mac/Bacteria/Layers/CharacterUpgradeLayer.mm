//
//  CharacterUpgradeLayer.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 6/22/13.
//
//

#import "CharacterUpgradeLayer.h"
#import "MainMenuLayer.h"
#import "Utils.h"
#import "GameSettingsManager.h"
#import "CharacterPropertiesManager.h"
#import "NSData+AES256.h"
#import "CoreSettings.h"
#import "StoreLayer.h"
//
@interface CharacterUpgradeLayer (PrivateMethods)
-(void) alertViewForGoToStore;
- (void) alertViewAction:(NSAlert *)alert returnCode:(int)returnCode contextInfo:(void *)contextInfo;
@end
//
@implementation CharacterUpgradeLayer
+(CCScene*) scene
{
    CCScene *scene = [CCScene node];
	CharacterUpgradeLayer *layer = [CharacterUpgradeLayer node];
	[scene addChild: layer];
	return scene;
}
//
-(id) init
{
    if(self = [super init])
    {
        //
        self.isKeyboardEnabled = YES;
        self.isMouseEnabled = YES;
        //
        [self createLayerMenu];
        //
        return self;
    }
    return nil;
}
//
-(void) alertViewForGoToStore
{
    //
    NSString *question = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_NO_VITAMINS];
    NSString *info =@"";
    NSString *cancelButton = @"Ok!";
    //
    NSAlert *alert = [[NSAlert alloc] init];
    [alert setMessageText:question];
    [alert setInformativeText:info];
    [alert addButtonWithTitle:cancelButton];
    
   [alert beginSheetModalForWindow:[[CoreSettings sharedCoreSettings] mainWindow]
                     modalDelegate:self
                    didEndSelector:@selector(alertViewAction:returnCode:contextInfo:)
                       contextInfo:nil];
    //
}
//
- (void) alertViewAction:(NSAlert *)alert returnCode:(int)returnCode contextInfo:(void *)contextInfo
{
    // this is just delegate, nothing to do.
}
//
-(void) createLayerMenu
{
    //
    priceLabelsArray = [[NSMutableArray alloc] init];
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    //
    //
    inAppKeysArray = [[NSArray arrayWithObjects:@"",
                       WATER_SHOOT_IS_BOUGHT_KEY,
                               WINDMILL_SHOOT_IS_BOUGHT_KEY,
                               CIRCLE_SHOOT_IS_BOUGHT_KEY,
                               TORNADO_SHOOT_IS_BOUGHT_KEY,
                               DOUBLE_SPEED_IS_BOUGHT_KEY,
                               IMMORTALITY_IS_BOUGHT_KEY,nil] retain];
    
    //
    inAppValuesArray = [[NSArray arrayWithObjects:@"",
                         WATER_SHOOT_IS_BOUGHT_VALUE,
                                 WINDMILL_SHOOT_IS_BOUGHT_VALUE,
                                 CIRCLE_SHOOT_IS_BOUGHT_VALUE,
                                 TORNADO_SHOOT_IS_BOUGHT_VALUE,
                                 DOUBLE_SPEED_IS_BOUGHT_VALUE,
                                 IMMORTALITY_IS_BOUGHT_VALUE,nil] retain];
    //
    NSData *emptyStringData = [[NSKeyedArchiver archivedDataWithRootObject:inAppValuesArray[0]]
                               AES256Encrypt];
    //
    NSData *waterShootIsBoughtValue = [[NSKeyedArchiver archivedDataWithRootObject:inAppValuesArray[1]]
                                       AES256Encrypt];
    //
    NSData *windmillShootIsBoughtValue = [[NSKeyedArchiver archivedDataWithRootObject:inAppValuesArray[2]]
                                          AES256Encrypt];
    //
    NSData *circleShootIsBoughtValue = [[NSKeyedArchiver archivedDataWithRootObject:inAppValuesArray[3]]
                                        AES256Encrypt];
    //
    NSData *tornadoShootIsBoughtValue = [[NSKeyedArchiver archivedDataWithRootObject:inAppValuesArray[4]]
                                         AES256Encrypt];
    //
    NSData *doubleSpeedIsBoughtValue = [[NSKeyedArchiver archivedDataWithRootObject:inAppValuesArray[5]]
                                   AES256Encrypt];
    //
    NSData *immortalityIsBoughtValue = [[NSKeyedArchiver archivedDataWithRootObject:inAppValuesArray[6]]
                                        AES256Encrypt];
    //
    //
    //
    encriptedValuesArray = [[NSArray arrayWithObjects:emptyStringData,
                                                     waterShootIsBoughtValue,
                                                     windmillShootIsBoughtValue,
                                                     circleShootIsBoughtValue,
                                                     tornadoShootIsBoughtValue,
                                                     doubleSpeedIsBoughtValue,
                                                     immortalityIsBoughtValue,nil] retain];
    //
    NSArray *vitaminsValues = [NSArray arrayWithObjects:@"",
                                                        @"20000",
                                                        @"20000",
                                                        @"20000",
                                                        @"20000",
                                                        @"40000",
                                                        @"1000000",nil];
    //
    arrawyWithExchangeValues = [[NSMutableArray alloc] initWithArray:vitaminsValues];
    //
    float aspectRatio = [[CoreSettings sharedCoreSettings] aspectRatio];
    //
    buyButtons = [[NSMutableArray alloc] init];
    //
    
    CCLabelTTF *Title = [CCLabelTTF labelWithString:[[LocalizationManager sharedLocalizationManager]
                                                     getValueWithKey:_UPGRADE_SOME_FEATURES]
                                         dimensions:CGSizeMake(500*aspectRatio,100*aspectRatio)
                                         hAlignment:kCCTextAlignmentCenter
                                           fontName:@"Times New Roman"
                                           fontSize:40*aspectRatio];
    //
    Title.position = ccp([CCDirector sharedDirector].winSize.width / 2.0f,
                         [CCDirector sharedDirector].winSize.height - 150*aspectRatio);
    Title.color = ccc3(0, 255, 0);
    [self addChild:Title];
    //
    //
    NSString *vitaminsString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_VITAMINS];
    vitaminsString = [vitaminsString stringByAppendingString:[NSString stringWithFormat:@" %d",
                                                              [[GameSettingsManager sharedGameSettingsManager] vitaminsCount]]];
    //
    vitaminsCountLabel = [CCLabelTTF labelWithString:[NSString stringWithFormat:vitaminsString,
                                                      [[GameSettingsManager sharedGameSettingsManager] vitaminsCount]]
                                         dimensions:CGSizeMake(500*aspectRatio,100*aspectRatio)
                                         hAlignment:kCCTextAlignmentCenter
                                           fontName:@"Times New Roman"
                                           fontSize:40*aspectRatio];
    //
    vitaminsCountLabel.position = ccp([CCDirector sharedDirector].winSize.width / 2.0f,
                         [CCDirector sharedDirector].winSize.height - 300*aspectRatio);
    vitaminsCountLabel.color = ccc3(0, 255, 255);
    [self addChild:vitaminsCountLabel];
    //
    //
    //
    NSString *array[7] = {
        [[LocalizationManager sharedLocalizationManager] getValueWithKey:_NORMAL_SHOOT],
        [[LocalizationManager sharedLocalizationManager] getValueWithKey:_WATER_SHOOT],
        [[LocalizationManager sharedLocalizationManager] getValueWithKey:_WINDMLL_SHOOT],
        [[LocalizationManager sharedLocalizationManager] getValueWithKey:_CIRCLE_SHOOT],
        [[LocalizationManager sharedLocalizationManager] getValueWithKey:_TORNADO_SHOOT],
        [[LocalizationManager sharedLocalizationManager] getValueWithKey:_DOUBLE_SPEED],
        [[LocalizationManager sharedLocalizationManager] getValueWithKey:_IMMORTALITY]};
    //
    float startX = [CCDirector sharedDirector].winSize.width / 2.0f -
    [CCDirector sharedDirector].winSize.width / 4.0f;
    //
    float startY = [CCDirector sharedDirector].winSize.height / 2.0f +
    [CCDirector sharedDirector].winSize.height / 8.0f;
    //
    float stepY = 80.0f*aspectRatio;
    //
    for (int i =0 ; i<arrawyWithExchangeValues.count; i++)
    {
        CCLabelTTF *titleItem = [CCLabelTTF labelWithString:array[i]
                                                 dimensions:CGSizeMake(500*aspectRatio,100*aspectRatio)
                                                 hAlignment:kCCTextAlignmentCenter
                                                   fontName:@"Times New Roman"
                                                   fontSize:40*aspectRatio];
        //
        titleItem.position = ccp(startX,startY);
        titleItem.color = ccc3(0, 255, 0);
        [self addChild:titleItem];
        //
        CCSprite *buySprite = [CCSprite spriteWithFile:@"BuyButton.png"];
        buySprite.position = ccp([CCDirector sharedDirector].winSize.width / 2.0f +
                                 [CCDirector sharedDirector].winSize.width / 6.0f,
                                 startY + buySprite.boundingBox.size.height / 8.0f);
        [self addChild:buySprite];
        buySprite.tag = i;
        buySprite.scale = aspectRatio;
        //
       
        //
        if((int)[[CharacterPropertiesManager sharedCharacterPropertiesManager] currentShootType] == i)
            buySprite.color = ccc3(0, 0, 255);
        //
        NSString *key = inAppKeysArray[i];
        NSData *dataValue = [defaults objectForKey:key];
        NSString *value = [[[NSString alloc] initWithData:[dataValue AES256Decrypt] encoding:NSUTF8StringEncoding] autorelease];
        //NSLog(@"value = %@",value);
        //
        CCLabelTTF *priceLabel = [CCLabelTTF labelWithString:arrawyWithExchangeValues[i]
                                                dimensions:CGSizeMake(500*aspectRatio,100*aspectRatio)
                                                hAlignment:kCCTextAlignmentLeft
                                                fontName:@"Times New Roman"
                                                fontSize:40*aspectRatio];
        //
        priceLabel.position = ccp(buySprite.position.x + 300*aspectRatio,buySprite.position.y - 25*aspectRatio);
        priceLabel.color = ccc3(0, 255, 0);
        [self addChild:priceLabel];
        [priceLabelsArray addObject:priceLabel];
        //
        NSString *valueString = (![value isEqualToString:inAppValuesArray[i]]) ? arrawyWithExchangeValues[i] : @"";
        //
        priceLabel.string = valueString;
        //NSLog(@"price Value string = %@",valueString);
        //}
        //
        //
        if(i> 0 && i < 5)   // shoot types
        {
            //
            NSString *key = inAppKeysArray[i];
            NSData *dataValue = [defaults objectForKey:key];
            NSString *value = [[[NSString alloc] initWithData:[dataValue AES256Decrypt] encoding:NSUTF8StringEncoding] autorelease];
            //
            if([value isEqualToString:inAppValuesArray[i]])
            {
                arrawyWithExchangeValues[i] = @"";
                ((CCLabelTTF*)priceLabelsArray[i]).string = arrawyWithExchangeValues[i];
            }
        }
        if(i == 5 || i == 6)    // double speed and immortality
        {
            //
            NSString *key = inAppKeysArray[i];
            NSData *dataValue = [defaults objectForKey:key];
            NSString *value = [[[NSString alloc] initWithData:[dataValue AES256Decrypt] encoding:NSUTF8StringEncoding] autorelease];
            //
            if([value isEqualToString:inAppValuesArray[i]])
            {
                arrawyWithExchangeValues[i] = @"";
                ((CCLabelTTF*)priceLabelsArray[i]).string = arrawyWithExchangeValues[i];
                if(i == 5)
                    buySprite.color = ([[CharacterPropertiesManager sharedCharacterPropertiesManager] isDoubleSpeed]) ? ccc3(255, 0, 0) : ccc3(255, 255, 255);
                if(i == 6)
                    buySprite.color = ([[CharacterPropertiesManager sharedCharacterPropertiesManager] isImmortal]) ? ccc3(255, 0, 0) : ccc3(255, 255, 255);
            }
        }
        //
        //
        [buyButtons addObject:buySprite];
        //
        startY-=stepY;
    }
}
//
//
-(BOOL) ccKeyDown:(NSEvent *)event
{
    //
    NSString *key = [event characters];
    unichar keyCode = [key characterAtIndex: 0];
    if(keyCode == ESC_KEY)
    {
        [[CCDirector sharedDirector] replaceScene:[MainMenuLayer scene]];
    }
    return YES;
    //
}
//
-(BOOL) ccMouseDown:(NSEvent *)event
{
    //
    CGPoint mousePoints = [[CCDirector sharedDirector] convertEventToGL:event];
    //
    for (int i = 0; i<(int)buyButtons.count; i++) {
        CCSprite *button = (CCSprite*)[buyButtons objectAtIndex:i];
        if(button.opacity == 255)
        {
            BOOL isInButton = [Utils pointInRect:mousePoints andRect:button.boundingBox];
            if (isInButton) {
                //NSLog(@"button Index = %d",(int)button.tag);
                //
                if(i>=0 && i<5)
                {
                    
                    //
                    // decript here
                    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
                    NSString *key = inAppKeysArray[i];
                    NSData *dataValue = [defaults objectForKey:key];
                    NSString *value = [[[NSString alloc] initWithData:[dataValue AES256Decrypt] encoding:NSUTF8StringEncoding] autorelease];
                    NSLog(@"value to bought = %@",value);
                    //
                    if([value isEqualToString:inAppValuesArray[i]] == NO)
                    {
                        //
                        //
                        int currentVitaminsCount = [[GameSettingsManager sharedGameSettingsManager] vitaminsCount];
                        if(currentVitaminsCount >= [arrawyWithExchangeValues[i] intValue])
                        {
                            //
                            for(int j = 0; j < 5;j++)
                                ((CCSprite*)buyButtons[j]).color = ccc3(255, 255, 255);
                            button.color = ccc3(0, 0, 255);
                            //
                            currentVitaminsCount-=[arrawyWithExchangeValues[i] intValue];
                            [[GameSettingsManager sharedGameSettingsManager] setVitaminsCount:currentVitaminsCount];
                            [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
                            //
                            arrawyWithExchangeValues[button.tag] = @"";
                            ((CCLabelTTF*)priceLabelsArray[button.tag]).string = arrawyWithExchangeValues[button.tag];
                            //
                            // encript here
                            NSData *encriptedValue =  [[inAppValuesArray[i] dataUsingEncoding:NSUTF8StringEncoding] AES256Encrypt];
                            [defaults setObject:encriptedValue forKey:key];
                            [defaults synchronize];
                            //
                            ShootType shootType = (ShootType)(button.tag);
                            [[CharacterPropertiesManager sharedCharacterPropertiesManager] setCurrentShootType:shootType];
                            [[CharacterPropertiesManager sharedCharacterPropertiesManager] saveProperties];
                            //
                        }
                        else {
                            [self alertViewForGoToStore];
                        }
                    }
                    //
                    else {
                        for(int j = 0; j < 5;j++)
                            ((CCSprite*)buyButtons[j]).color = ccc3(255, 255, 255);
                        button.color = ccc3(0, 0, 255);
                        //
                        ShootType shootType = (ShootType)(button.tag);
                        [[CharacterPropertiesManager sharedCharacterPropertiesManager] setCurrentShootType:shootType];
                        [[CharacterPropertiesManager sharedCharacterPropertiesManager] saveProperties];
                        //
                    }
                    //
                }
                //
                else if(i == 5) // get double speed
                {
                                        //
                    // decript here
                    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
                    NSString *key = inAppKeysArray[i];
                    NSData *dataValue = [defaults objectForKey:key];
                    NSString *value = [[[NSString alloc] initWithData:[dataValue AES256Decrypt] encoding:NSUTF8StringEncoding] autorelease];
                    //NSLog(@"value to bought = %@",value);
                    //
                    if([value isEqualToString:inAppValuesArray[i]] == NO)
                    {
                        //
                        //
                        int currentVitaminsCount = [[GameSettingsManager sharedGameSettingsManager] vitaminsCount];
                        if(currentVitaminsCount >= [arrawyWithExchangeValues[i] intValue])
                        {
                            //
                            BOOL isDoubleSpeed = [[CharacterPropertiesManager sharedCharacterPropertiesManager] isDoubleSpeed];
                            button.color = (isDoubleSpeed) ? ccc3(255, 255, 255) : ccc3(255, 0, 0);
                            [[CharacterPropertiesManager sharedCharacterPropertiesManager] setIsDoubleSpeed:(!isDoubleSpeed)];
                            [[CharacterPropertiesManager sharedCharacterPropertiesManager] saveProperties];
                            //
                            currentVitaminsCount-=[arrawyWithExchangeValues[i] intValue];
                            [[GameSettingsManager sharedGameSettingsManager] setVitaminsCount:currentVitaminsCount];
                            [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
                            //
                            arrawyWithExchangeValues[button.tag] = @"";
                            ((CCLabelTTF*)priceLabelsArray[button.tag]).string = arrawyWithExchangeValues[button.tag];
                            //
                            // encript here
                            NSData *encriptedValue =  [[inAppValuesArray[i] dataUsingEncoding:NSUTF8StringEncoding] AES256Encrypt];
                            [defaults setObject:encriptedValue forKey:key];
                            [defaults synchronize];
                            //
                            [[CharacterPropertiesManager sharedCharacterPropertiesManager] saveProperties];
                            //
                        }
                        else {
                            [self alertViewForGoToStore];
                        }
                    }
                    //
                    else {
                        BOOL isDoubleSpeed = [[CharacterPropertiesManager sharedCharacterPropertiesManager] isDoubleSpeed];
                        button.color = (isDoubleSpeed) ? ccc3(255, 255, 255) : ccc3(255, 0, 0);
                        [[CharacterPropertiesManager sharedCharacterPropertiesManager] setIsDoubleSpeed:(!isDoubleSpeed)];
                        [[CharacterPropertiesManager sharedCharacterPropertiesManager] saveProperties];
                    }
                    //
                }
                //
                else if(i == 6) // get immortality
                {
                    // decript here
                    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
                    NSString *key = inAppKeysArray[i];
                    NSData *dataValue = [defaults objectForKey:key];
                    NSString *value = [[[NSString alloc] initWithData:[dataValue AES256Decrypt]
                                                             encoding:NSUTF8StringEncoding] autorelease];
                    //
                    if([value isEqualToString:inAppValuesArray[i]] == NO)
                    {
                        //
                        //
                        int currentVitaminsCount = [[GameSettingsManager sharedGameSettingsManager] vitaminsCount];
                        if(currentVitaminsCount >= [arrawyWithExchangeValues[i] intValue])
                        {
                            BOOL isImmortal = [[CharacterPropertiesManager sharedCharacterPropertiesManager] isImmortal];
                            button.color = (isImmortal) ? ccc3(255, 255, 255) : ccc3(255, 0, 0);
                            [[CharacterPropertiesManager sharedCharacterPropertiesManager] setIsImmortal:(!isImmortal)];
                            [[CharacterPropertiesManager sharedCharacterPropertiesManager] saveProperties];
                            //
                            currentVitaminsCount-=[arrawyWithExchangeValues[i] intValue];
                            [[GameSettingsManager sharedGameSettingsManager] setVitaminsCount:currentVitaminsCount];
                            [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
                            //
                            arrawyWithExchangeValues[button.tag] = @"";
                            ((CCLabelTTF*)priceLabelsArray[button.tag]).string = arrawyWithExchangeValues[button.tag];
                            //
                            // encript here
                            NSData *encriptedValue =  [[inAppValuesArray[i] dataUsingEncoding:NSUTF8StringEncoding] AES256Encrypt];
                            [defaults setObject:encriptedValue forKey:key];
                            [defaults synchronize];
                            //
                            [[CharacterPropertiesManager sharedCharacterPropertiesManager] saveProperties];
                            //
                        }
                        else {
                            [self alertViewForGoToStore];
                        }
                        
                    }
                    //
                    else {
                        BOOL isImmortal = [[CharacterPropertiesManager sharedCharacterPropertiesManager] isImmortal];
                        button.color = (isImmortal) ? ccc3(255, 255, 255) : ccc3(255, 0, 0);
                        [[CharacterPropertiesManager sharedCharacterPropertiesManager] setIsImmortal:(!isImmortal)];
                        [[CharacterPropertiesManager sharedCharacterPropertiesManager] saveProperties];
                    }
                    //
                }
                //
                NSString *vitaminsString = [[LocalizationManager sharedLocalizationManager] getValueWithKey:_VITAMINS];
                vitaminsString = [vitaminsString stringByAppendingString:[NSString stringWithFormat:@" %d",
                                                                          [[GameSettingsManager sharedGameSettingsManager] vitaminsCount]]];
                //
                vitaminsCountLabel.string = vitaminsString;
                //
                return YES;
            }
        }
    }
    //
    return YES;
}
//
//
-(void) onExit
{
    self.isKeyboardEnabled = NO;
    self.isMouseEnabled = NO;
    [priceLabelsArray release];
    priceLabelsArray = nil;
    [arrawyWithExchangeValues release];
    arrawyWithExchangeValues = nil;
    [inAppKeysArray release];
    inAppKeysArray = nil;
    [inAppValuesArray release];
    inAppValuesArray = nil;
}
//
//
@end
