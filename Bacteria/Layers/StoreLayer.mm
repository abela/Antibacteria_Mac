//
//  StoreLayer.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "StoreLayer.h"
#import "Utils.h"
#import "MainMenuLayer.h"
#import "GameSettingsManager.h"
//
#define kVitaminsId_1               @"com.antiBacteria.vitamins.1"
#define kVitaminsId_2               @"com.antiBacteria.vitamins.2"
#define kVitaminsId_3               @"com.antiBacteria.vitamins.3"
#define kVitaminsId_4               @"com.antiBacteria.vitamins.4"
//

@interface StoreLayer (PrivateMethods)
-(void) createVitaminStoreMenuToBuy:(NSNotification*)notification;
@end
//
@implementation StoreLayer
+(CCScene*)scene
{
	CCScene *scene = [CCScene node];
	StoreLayer *layer = [StoreLayer node];
	[scene addChild: layer];
	return scene;
}
//
-(id) init
{
    if((self = [super init]))
    {
        [self createLayerMenu];
        self.isMouseEnabled = YES;
        self.isKeyboardEnabled = YES;
        //
        [[SKPaymentQueue defaultQueue] addTransactionObserver:self];
        //
        return self;
    }
    return nil;
}
//
-(void) showMenu:(BOOL)showMenuFlag
{
    if (showMenuFlag) {
        for (int i = 0; i<(int)buyButtons.count; i++) {
            CCSprite *button = (CCSprite*)[buyButtons objectAtIndex:i];
            button.opacity = 255;
            loadingLabel.string = @"Buy Extra Vitamins";
        }
    }
    else {
        for (int i = 0; i<(int)buyButtons.count; i++) {
            CCSprite *button = (CCSprite*)[buyButtons objectAtIndex:i];
            button.opacity = 128;
            loadingLabel.string  = @"Buying...";
        }
    }
    //
}
//
-(void) createVitaminStoreMenuToBuy:(NSNotification*)notification
{
}
//
-(void) createLayerMenu
{
    //
    buyButtons = [[NSMutableArray alloc] init];
    //
    float aspectRatio = [[CoreSettings sharedCoreSettings] aspectRatio];
    //
    NSString *vitaminsString = @"";
    vitaminsString = [vitaminsString stringByAppendingString:[[LocalizationManager sharedLocalizationManager]
                                                              getValueWithKey:_VITAMINS]];
    //
    vitaminsString = [vitaminsString stringByAppendingString:[NSString stringWithFormat:@" %d ",
                                                              [[GameSettingsManager sharedGameSettingsManager]
                                                               vitaminsCount]]];
    //
    vitaminsCountTitle = [CCLabelTTF labelWithString:[NSString stringWithFormat:vitaminsString,
                                                      [[GameSettingsManager sharedGameSettingsManager] vitaminsCount]]
                                             dimensions:CGSizeMake(500*aspectRatio, 100*aspectRatio)
                                             hAlignment:kCCTextAlignmentCenter
                                               fontName:@"Times New Roman"
                                               fontSize:40*aspectRatio];
    //
    vitaminsCountTitle.position = ccp([CCDirector sharedDirector].winSize.width / 2.0f,
                                      [CCDirector sharedDirector].winSize.height / 2.0f + 350.0f*aspectRatio);
    vitaminsCountTitle.color = ccc3(0, 255, 0);
    [self addChild:vitaminsCountTitle];
    //
    //
    
    productIdsArray = [[NSArray alloc] initWithObjects:@"com.antiBacteria.vitamins.1",
                                                       @"com.antiBacteria.vitamins.2",
                                                       @"com.antiBacteria.vitamins.3",
                                                       @"com.antiBacteria.vitamins.4", nil];
    //
    
    //
    NSString *pricestr1 = @"10 000 ";
    pricestr1 = ([[LocalizationManager sharedLocalizationManager] currentLocalization] == kRussianLocalization) ?
                 [pricestr1 stringByAppendingString:@"Витаминов"] : [pricestr1 stringByAppendingString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_VITAMINS]];
    //
    NSString *pricestr2 = @"30 000 ";
    pricestr2 = ([[LocalizationManager sharedLocalizationManager] currentLocalization] == kRussianLocalization) ?
                 [pricestr2 stringByAppendingString:@"Витаминов"] : [pricestr2 stringByAppendingString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_VITAMINS]];
    //
    NSString *pricestr3 = @"100 000 ";
    pricestr3 = ([[LocalizationManager sharedLocalizationManager] currentLocalization] == kRussianLocalization) ?
                 [pricestr3 stringByAppendingString:@"Витаминов"] : [pricestr3 stringByAppendingString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_VITAMINS]];
    //
    NSString *pricestr4 = @"1 000 000 ";
    pricestr4 = ([[LocalizationManager sharedLocalizationManager] currentLocalization] == kRussianLocalization) ?
                 [pricestr4 stringByAppendingString:@"Витаминов"] : [pricestr4 stringByAppendingString:[[LocalizationManager sharedLocalizationManager] getValueWithKey:_VITAMINS]];
    //
    //
    NSArray *titlesArray = [NSArray arrayWithObjects:pricestr1,
                                                     pricestr2,
                                                     pricestr3,
                                                     pricestr4,nil];
    //
    NSArray *pricesArray = [NSArray arrayWithObjects:@"$ 0.99",
                                                     @"$ 1.99",
                                                     @"$ 3.99",
                                                     @"$ 9.99",nil];
    //
    float startX = [CCDirector sharedDirector].winSize.width / 2.0f -
    [CCDirector sharedDirector].winSize.width / 8.0f;
    //
    float startY = [CCDirector sharedDirector].winSize.height / 2.0f +
    [CCDirector sharedDirector].winSize.height / 8.0f;
    //
    float stepY = 80.0f*aspectRatio;
    //
    for (int i = 0; i<4; i++)
    {
        //
        CCLabelTTF *titleItem = [CCLabelTTF labelWithString:titlesArray[i]
                                                 dimensions:CGSizeMake(500*aspectRatio, 100*aspectRatio)
                                                 hAlignment:kCCTextAlignmentLeft
                                                   fontName:@"Times New Roman"
                                                   fontSize:40*aspectRatio];
        //
        titleItem.position = ccp(startX,startY);
        titleItem.color = ccc3(0, 255, 0);
        [self addChild:titleItem];
        //
        //
        // buy button
        CCSprite *buySprite = [CCSprite spriteWithFile:@"BuyButton.png"];
        buySprite.position = ccp([CCDirector sharedDirector].winSize.width / 2.0f +
                                 [CCDirector sharedDirector].winSize.width / 6.0f,
                                 startY + buySprite.boundingBox.size.height / 4.0f);
        [self addChild:buySprite];
        buySprite.tag = i;
        buySprite.scale = aspectRatio;
        //
        //
        // price
        CCLabelTTF *priceItem = [CCLabelTTF labelWithString:pricesArray[i]
                                                 dimensions:CGSizeMake(500*aspectRatio, 100*aspectRatio)
                                                 hAlignment:kCCTextAlignmentRight
                                                   fontName:@"Times New Roman"
                                                   fontSize:40*aspectRatio];
        //
        priceItem.position = ccp([CCDirector sharedDirector].winSize.width / 2.0f +
                                 [CCDirector sharedDirector].winSize.width / 8.0f,startY);
        priceItem.color = ccc3(0, 255, 0);
        [self addChild:priceItem];
        //
        //
        [buyButtons addObject:buySprite];
        //
        //
        startY-=stepY;
    }
    //
    //[[InAppManager sharedInAppManager] startGettingProducts];
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
            if (isInButton)
            {
                NSLog(@"button Index = %d",(int)button.tag);
                NSLog(@"indetifier = %@", productIdsArray[button.tag]);
                //
                NSString *productIdentifier = productIdsArray[button.tag];
                [self buyProductWithIndentifier:productIdentifier];
                //
                return YES;
            }
        }
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
-(void) onExit
{
    self.isKeyboardEnabled = NO;
    self.isMouseEnabled = NO;
}


- (void) buyProductWithIndentifier:(NSString *) identifier
{
    //
    NSSet             *productIndentifierSet = [NSSet setWithObject:identifier];
    SKProductsRequest *productRequest        = [[SKProductsRequest alloc] initWithProductIdentifiers:productIndentifierSet];
    productRequest.delegate                  = self;
    [productRequest start];
    [productRequest release];
    //
}

/** recived products */
- (void) productsRequest:(SKProductsRequest *)request didReceiveResponse:(SKProductsResponse *)response
{
    NSArray *products = response.products;
    for(SKProduct *product in products)
    {
        [[SKPaymentQueue defaultQueue] addPayment:[SKPayment paymentWithProduct:product]];
    }
}

- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray *)transactions
{
    for (SKPaymentTransaction *transaction in transactions)
    {
        switch (transaction.transactionState)
        {
            case SKPaymentTransactionStatePurchasing:
                NSLog(@"Purchasing!");
                break;
            case SKPaymentTransactionStatePurchased:
                /** Product puchased */
                
            {
                int vitaminsToAdd = 0;
                if([transaction.payment.productIdentifier isEqualToString:kVitaminsId_1])
                {
                    vitaminsToAdd = 10000;
                } else  if([transaction.payment.productIdentifier isEqualToString:kVitaminsId_2]) {
                    vitaminsToAdd = 30000;
                } else if([transaction.payment.productIdentifier isEqualToString:kVitaminsId_3]) {
                    vitaminsToAdd = 100000;
                } else if([transaction.payment.productIdentifier isEqualToString:kVitaminsId_4]) {
                    vitaminsToAdd = 1000000;
                }
                
                [GameSettingsManager sharedGameSettingsManager].vitaminsCount = vitaminsToAdd;
                [[GameSettingsManager sharedGameSettingsManager] saveGameSettings];
                
                NSLog(@"Purchase Succeded!");
            }
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;
            case SKPaymentTransactionStateFailed:
                 NSLog(@"Purchase Failed!");
                /** Transaction failed */
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
                break;
            case SKPaymentTransactionStateRestored:
                NSLog(@"Purchase Restored!");
                /** There is not restore transaction in the game */
                [[SKPaymentQueue defaultQueue] finishTransaction:transaction];
            default:
                break;
        }
    }
}

- (void)paymentQueue:(SKPaymentQueue *)queue removedTransactions:(NSArray *)transactions
{
    
}

- (void)paymentQueueRestoreCompletedTransactionsFinished:(SKPaymentQueue *)queue
{
    
}

- (void)paymentQueue:(SKPaymentQueue *)queue updatedDownloads:(NSArray *)downloads
{
    
}

- (void) dealloc
{
    [super dealloc];
    //[[SKPaymentQueue defaultQueue] removeTransactionObserver:self];
    [productIdsArray release];
}
@end
