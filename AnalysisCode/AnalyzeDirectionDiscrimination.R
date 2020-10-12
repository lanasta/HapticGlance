library(hash)
library(crayon)
library("plotrix")

magnitudeThreshold <- 0.4
factorInAbsValue <- FALSE

calculateDirectionBasedErrorAngle <- function (angle) {
  if (direction == "ne" || direction == "se" || direction == "nw" || direction == "sw") {
    angle <- 45 - abs(angle)
  }else if (direction == "n") {
    angle <- 90 - abs(angle)
  } else if (direction == "s") {
    angle <- abs(angle) - 90
  }
  cat("\n",direction, angle)
  return (angle)
}

analyzeThresholdPoints <- function(mydata) {
  magnitude <- mydata$Magnitude
  firstPassPt <- mydata[min(which(magnitude >= magnitudeThreshold)),]
  secondPassPt <- mydata[max(which(magnitude >= magnitudeThreshold)),]
  peakPt <- mydata[which(magnitude == max(magnitude)),]
  firstPassAngle <- atan(firstPassPt$Fy/firstPassPt$Fx) * 180 / pi
  secondPassAngle <- atan(secondPassPt$Fy/secondPassPt$Fx) * 180 / pi
  peakPtAngle <- atan(peakPt$Fy/peakPt$Fx) * 180 / pi
  firstPassErrorAngle <- calculateDirectionBasedErrorAngle(firstPassAngle)
  secondPassErrorAngle <- calculateDirectionBasedErrorAngle(secondPassAngle)
  peakPtErrorAngle <- calculateDirectionBasedErrorAngle(peakPtAngle)
  newlist <- list(firstPassErrorAngle, secondPassErrorAngle, peakPtErrorAngle, firstPassAngle, secondPassAngle, peakPtAngle)
  return (newlist)
}

plotForceVectorLine <- function(Fx, Fy, colorRandom) {
  par(new = TRUE)
  plot(Fx, Fy, ylim=range(c(-1.5,1.5)),  xlim=c(-1.5,1.5), type="l", axes = FALSE, xlab = "", ylab = "", col = colorRandom)
}

clipBasedOnDirection <- function(direction){
  x1 <- -1
  x2 <- 1
  y1 <- -1
  y2 <- 1
  if (direction == "n") {
    x1<- -1
    x2 <- 1
    y1 <- 0
  } else if (direction == "s") {
    x1<- -1
    x2 <- 1
    y2 <- 0
  } else if (direction == "e") {
    x1 <- 0
  } else if (direction == "w") {
    x2 <- 0
  } else if (direction == "ne") {
    y1 <- 0
    x1 <- 0
  } else if (direction == "se") {
    y2 <- 0
    x1 <- 0
  } else if (direction == "sw") {
    y2 <- 0
    x2 <- 0
  } else if (direction == "nw") {
    y1 <- 0
    x2 <- 0
  }
  clip(x1, x2, y1, y2)
}

analyzeRegressionLine <- function(Fx, Fy, color) {
  #axis lines
  regressionErrorAngle <- 0
  if (direction == "se" || direction == "nw"){
    abline(0,-1, col="blue")
  } else if (direction == "sw" || direction == "ne"){
    abline(0,1, col="blue")
  } else {
    if (direction == "n" || direction == "s") {
      abline(v=0,  col="blue")
    } else {
      #regression and expected line
      abline(0, 0,  col="blue")
    }
  }
  clipBasedOnDirection(direction)
  fit.lm <-lm(Fy ~ Fx)
  abline(lm(Fy~Fx), col=color, lwd=2)
  slope <- coef(fit.lm)[2]
  regressionAngle <-  atan(slope) * 180 / pi
  regressionErrorAngle <- calculateDirectionBasedErrorAngle(regressionAngle)
  cat("\nRegression angle:", atan(slope) * 180 / pi)
  newlist <- list(regressionErrorAngle, as.numeric(atan(slope) * 180 / pi))
  return (newlist)
}


plotForceVector <- function(direction){
  plot(0, 0, main=paste("2D Force Vector", toupper(direction)), xlab="Fx", ylab="Fy", xlim=c(-1.5,1.5), ylim=c(-1.5, 1.5))
  abline(0, 0, col="black")
  abline(v=0, col="black")
  fileCount <- 0
  for (filename in myFiles) {
    fileCount <- fileCount + 1
    ds <- read.csv(filename)
    name <- basename(filename)
    Fx <- ds$Fx
    Fy <- ds$Fy
    mydata <- ds[ds$Magnitude > 0.10,]
    magnitude <- mydata$Magnitude
    colorRandom <- getRandomColor(FALSE)
    plotForceVectorLine(Fx, Fy, colorRandom)
    regressionAngles <- analyzeRegressionLine(Fx,Fy,colorRandom)
    regressionErrorList <- c(regressionErrorList, calcByAbsMode(as.numeric(regressionAngles[1]), 1))
    regAngles <- c(regAngles, as.numeric(regressionAngles[2]))
    h[direction] <- regressionErrorList
  }
  print(regAngles)
  cat("\nAverage regression error:", mean(regressionErrorList), "\n")
  regAnglesDir[direction] <- mean(regAngles)
}

plotMagnitude <- function(direction) {
  first <- TRUE
  fileCount <- 0
  for (filename in myFiles) {
    ds <- read.csv(filename)
    mydata <- ds[ds$Magnitude > 0.14,]
    
    fileCount <- fileCount + 1
    timestamp <- mydata$Timestamp
    magnitude <- mydata$Magnitude
    #firstThreshPass<- min(which(magnitude >= magnitudeThreshold))
    #beforeFirstPass <- max(which(magnitude >= 0.14))
    
    #get point where graph crosses threshold for the first and last time, and peak point
    errorAnglesList <- analyzeThresholdPoints(mydata)
    firstThresholdPassErrorList <- c(firstThresholdPassErrorList, calcByAbsMode(as.numeric(errorAnglesList[1]), 1))
    secondThresholdPassErrorList <- c(secondThresholdPassErrorList, calcByAbsMode(as.numeric(errorAnglesList[2]), 1))
    peakPointErrorList <- c(peakPointErrorList, calcByAbsMode(as.numeric(errorAnglesList[3]), 1))
    firstPassAngles <- c(firstPassAngles, abs(as.numeric(errorAnglesList[4])))
    secondPassAngles <- c(secondPassAngles, as.numeric(errorAnglesList[5]))
    peakPtAngles <- c(peakPtAngles, as.numeric(errorAnglesList[6]))

    h1[direction] <- firstThresholdPassErrorList
    h2[direction] <- secondThresholdPassErrorList
    h3[direction] <- peakPointErrorList
    firstPassAnglesDir[direction] <- mean(firstPassAngles)
    secondPassAnglesDir[direction] <- mean(secondPassAngles)
    peakPtAnglesDir[direction] <- mean(peakPtAngles)
    magnitudeAverage[direction] <- mean(magnitude)
    
    colorRandom <- getRandomColor(FALSE)
    if (first) {
      plot(timestamp, magnitude, main=paste("Magnitude", toupper(direction)), ylim=c(0, 1.4), xlab="ms", ylab="N", type="l",  col=colorRandom)
      abline(magnitudeThreshold, 0, col="brown", lwd=2)
      first <- FALSE
    } else {
      par(new = TRUE)
      plot(timestamp, magnitude, ylim=c(0, 1.5), xlab="", ylab="", xaxt='n', type="l", axes= FALSE, col=colorRandom )
    }
    if (fileCount == length(myFiles)) {
      cat("Average threshold first pass error:", mean(firstThresholdPassErrorList), "\n")
      cat("Average threshold second pass error:", mean(secondThresholdPassErrorList), "\n")
      cat("Average peak point error:", mean(peakPointErrorList), "\n")
    }
  }
}

getAdjustedAngle <- function(angle, direction){
  # if (direction == "se") {
  #   return (angle + 90)
  # } else if (direction == "e") {
  #   return(angle)
  # }
  return(angle)
}

getRandomColor <- function(opaque){
  opacity <- 0.7
  if (opaque == TRUE) {
    opacity <- 1
  }
  return(rgb(runif(1, 0.5, 1.0), runif(1, 0.2, 1.0), runif(1, 0.3, 1.0), opacity))
}

drawGuideLines <- function(direction) {
  abline(v=0, col="black")
  abline(h=0, col="black")
  if (direction == "n" || direction == "s") {
    abline(v=0, col="blue", lw=1)
  } else if (direction == "e" || direction == "w") {
    abline(h=0, col="blue", lw=1)
  } else if (direction == "ne" || direction == "sw") {
    abline(coef = c(0,1), col="blue", lw=1)
  } else {
    abline(coef = c(0,-1), col="blue", lw=1)
  }
}

calcByAbsMode <- function(errorAngle, pos) {
  angle <- errorAngle
  # if (pos == 1 && errorAngle >= 0 || pos == 0 && errorAngle < 0){
  #   angle <- errorAngle
  # }
  if (factorInAbsValue) {
      return(abs(angle))
  } else {
    return(angle)
  }
}

directions <- list("n", "ne", "e", "se", "s", "sw", "w", "nw")
par(mfcol=c(4,6))
par(pty="s")
par(mar=c(2,1,2,1))
h <- hash() 
h1 <- hash() 
h2 <- hash() 
h3 <- hash() 
h4 <- hash()
h5 <- hash()
h6 <- hash()
h7 <- hash()
firstPassAnglesDir <- hash()
secondPassAnglesDir <- hash()
peakPtAnglesDir <- hash()
regAnglesDir <- hash()
magnitudeAverage <- hash()
bpYLim <- 40 #box plot y-limit

for (direction in directions) {
  cat(blue("\nDirection: ", toupper(direction)))
  pattern <- paste("DirectionDiscrimination*_" , direction , "-*.csv", sep="")
  myFiles <- Sys.glob(pattern)
  regressionErrorList <- c()
  firstThresholdPassErrorList <- c()
  secondThresholdPassErrorList <- c()
  peakPointErrorList <- c()
  firstPassAngles <- c()
  secondPassAngles <- c()
  peakPtAngles <- c()
  regAngles <- c()
  
  plotForceVector(direction)
  plotMagnitude(direction)
}

print(firstPassAnglesDir)
print(secondPassAnglesDir)
print(peakPtAnglesDir)
print(regAnglesDir)
print(magnitudeAverage)

boxplot(h[["n"]], h[["ne"]], h[["e"]], h[["se"]], h[["s"]], h[["sw"]], h[["w"]], h[["nw"]],
        main = "Regression° (+) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(-bpYLim, bpYLim)
)

boxplot(h1[["n"]], h1[["ne"]], h1[["e"]], h1[["se"]], h1[["s"]], h1[["sw"]], h1[["w"]], h1[["nw"]],
        main = "1st T-Pass° (+) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(-bpYLim, bpYLim)
)


boxplot(h2[["n"]], h2[["ne"]], h2[["e"]], h2[["se"]], h2[["s"]], h2[["sw"]], h2[["w"]], h2[["nw"]],
        main = "2nd T-Pass° (+) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(-bpYLim, bpYLim)
)

boxplot(h3[["n"]], h3[["ne"]], h3[["e"]], h3[["se"]], h3[["s"]], h3[["sw"]], h3[["w"]], h3[["nw"]],
        main = "Peak Point° (+) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(-bpYLim, bpYLim)
)

plot(0, 0, main="Summary", xlim=c(-1.2,1.2), ylim=c(-1.2, 1.2))
abline(0, 0, col="grey")
abline(v=0, col="grey")
blueCol <- rgb(red = 0, green = 0, blue = 1, alpha = 0.4)

abline(0,-1, col=blueCol, lty = 2)
abline(0,1, col=blueCol, lty = 2)
abline(v=0,  col=blueCol, lty = 2)
abline(0, 0,  col=blueCol, lty = 2)
for (direction in directions) {
  mag <- magnitudeAverage[[direction]]
  angle <- firstPassAnglesDir[[direction]]
  x <- mag * cos(angle * pi / 180)
  y <- mag * sin(angle * pi / 180)
  if (direction == "n" || direction == "nw" || direction == "ne") {
    if (y < 0 ) {
      y <- 0 - y
    }
  } 
  if (direction == "s" || direction == "sw" || direction == "se") {
    if (y > 0 ) {
      y <- 0 - y
    }
  } 
  if (direction == "sw" || direction == "w" || direction == "nw") {
     if (x > 0) {
       x <- 0 - x
     }
  }
  if (direction == "se" || direction == "e" || direction == "ne") {
    if (x < 0) {
      x <- 0 - x
    }
  }
  cat("\n", direction, " avg magnitude:", mag, "avg angle:", angle, "(x,y):", x, y)
  par(new = TRUE)
  colorRandom <- getRandomColor(TRUE)
  points(x, y, col = colorRandom, pch=19, cex = 1, lty = "solid", lwd = 1, text(x, y, paste(direction), cex=0.95,pos=3))
}
draw.circle(0, 0, 0.4, nv = 1000, border=blueCol, lty = 2, lwd = 1)




